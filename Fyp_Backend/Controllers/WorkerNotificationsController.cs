using System.Security.Claims;
using Fyp_Backend.Models;
using Fyp_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fyp_Backend.Controllers;

[ApiController]
[Route("api/worker/notifications")]
[Authorize(Roles = "Worker")]
public sealed class WorkerNotificationsController(
    Fyp1Context db, WorkerNotificationService notifications,
    ILogger<WorkerNotificationsController> logger) : ControllerBase
{
    // Identity always comes from the authenticated JWT, never a query/body worker ID.
    private int? WorkerId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) && id > 0 ? id : null;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 30,
        [FromQuery] DateTime? snapshotAtUtc = null, CancellationToken cancellationToken = default)
    {
        if (WorkerId is not int workerId) return Unauthorized();
        if (skip < 0 || take < 1 || take > 100)
            return BadRequest(new { message = "Use skip >= 0 and take between 1 and 100." });
        if (snapshotAtUtc is DateTime supplied && (supplied == default || supplied > DateTime.UtcNow.AddMinutes(1)))
            return BadRequest(new { message = "Invalid notification snapshot timestamp." });
        try
        {
            if (!await db.Workers.AnyAsync(w => w.WorkerId == workerId, cancellationToken)) return NotFound();
            await notifications.SynchronizeAsync(workerId, cancellationToken);
            var snapshot = snapshotAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
            var query = db.WorkerNotifications.AsNoTracking()
                .Where(n => n.WorkerId == workerId && n.CreatedAtUtc <= snapshot);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(n => n.CreatedAtUtc)
                .ThenByDescending(n => n.NotificationKey).Skip(skip).Take(take)
                .Select(n => new
                {
                    key = n.NotificationKey,
                    n.Type,
                    n.SourceId,
                    n.InterviewId,
                    n.Title,
                    n.Message,
                    n.CreatedAtUtc,
                    n.ReadAtUtc,
                    isRead = n.ReadAtUtc != null
                }).ToListAsync(cancellationToken);
            var unreadCount = await UnreadAsync(workerId, cancellationToken);
            return Ok(new { items, unreadCount, total, hasMore = skip + items.Count < total, snapshotAtUtc = snapshot });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not load notifications for worker {WorkerId}", workerId);
            return StatusCode(500, new { message = "Notifications could not be loaded. Please try again." });
        }
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        if (WorkerId is not int workerId) return Unauthorized();
        try
        {
            if (!await db.Workers.AnyAsync(w => w.WorkerId == workerId, cancellationToken)) return NotFound();
            await notifications.SynchronizeAsync(workerId, cancellationToken);
            return Ok(new { unreadCount = await UnreadAsync(workerId, cancellationToken) });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not count notifications for worker {WorkerId}", workerId);
            return StatusCode(500, new { message = "Notifications could not be refreshed." });
        }
    }

    [HttpPost("{key}/read")]
    public async Task<IActionResult> Read(string key, CancellationToken cancellationToken)
    {
        if (WorkerId is not int workerId) return Unauthorized();
        if (key.Length > 96) return BadRequest();
        try
        {
            var item = await db.WorkerNotifications.SingleOrDefaultAsync(
                n => n.WorkerId == workerId && n.NotificationKey == key, cancellationToken);
            if (item == null) return NotFound(new { message = "Notification not found." });
            if (item.ReadAtUtc == null)
            {
                item.ReadAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
            return Ok(new { key = item.NotificationKey, item.ReadAtUtc, unreadCount = await UnreadAsync(workerId, cancellationToken) });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not mark notification read for worker {WorkerId}", workerId);
            return StatusCode(500, new { message = "Read status could not be saved. Please try again." });
        }
    }

    public sealed class ReadAllRequest
    {
        public DateTime SnapshotAtUtc { get; set; }
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll([FromBody] ReadAllRequest request, CancellationToken cancellationToken)
    {
        if (WorkerId is not int workerId) return Unauthorized();
        if (request.SnapshotAtUtc == default || request.SnapshotAtUtc > DateTime.UtcNow.AddMinutes(1))
            return BadRequest(new { message = "A valid loaded snapshot timestamp is required." });
        try
        {
            // Keep notifications arriving after the user's loaded snapshot unread.
            var items = await db.WorkerNotifications.Where(n => n.WorkerId == workerId &&
                n.ReadAtUtc == null && n.CreatedAtUtc <= request.SnapshotAtUtc.ToUniversalTime())
                .ToListAsync(cancellationToken);
            var now = DateTime.UtcNow;
            foreach (var item in items) item.ReadAtUtc = now;
            if (items.Count > 0) await db.SaveChangesAsync(cancellationToken);
            return Ok(new { markedRead = items.Count, readAtUtc = now, unreadCount = await UnreadAsync(workerId, cancellationToken) });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not mark notifications read for worker {WorkerId}", workerId);
            return StatusCode(500, new { message = "Read status could not be saved. Please try again." });
        }
    }

    private Task<int> UnreadAsync(int workerId, CancellationToken cancellationToken) =>
        db.WorkerNotifications.CountAsync(n => n.WorkerId == workerId && n.ReadAtUtc == null, cancellationToken);
}
