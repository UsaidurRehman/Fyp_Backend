using Fyp_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fyp_Backend.Controllers
{
    // Master habits + the habits a single worker picked.
    //
    //  GET  api/Habits/GetHabits                        → the checkbox list / filter options
    //  GET  api/Habits/GetWorkerHabits/{workerId}       → that worker's current selection
    //  POST api/Habits/UpdateWorkerHabits               → replace the worker's selection
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HabitsController : ControllerBase
    {
        private readonly Fyp1Context _context;

        public HabitsController(Fyp1Context context)
        {
            _context = context;
        }

        // ── Master list (used by signup, My Habits, the client filter and the profile tab) ──
        [HttpGet("GetHabits")]
        [AllowAnonymous] // the signup screen calls this before the worker has a token
        public async Task<IActionResult> GetHabits()
        {
            try
            {
                var habits = await _context.Habits
                    .Where(h => h.IsActive)
                    .OrderBy(h => h.SortOrder)
                    .ThenBy(h => h.Name)
                    .Select(h => new
                    {
                        h.HabitId,
                        h.Name,
                        h.SortOrder
                    })
                    .ToListAsync();

                return Ok(habits);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching habits: " + ex.Message });
            }
        }

        // ── What one worker selected ──
        [HttpGet("GetWorkerHabits/{workerId}")]
        public async Task<IActionResult> GetWorkerHabits(int workerId)
        {
            try
            {
                var workerExists = await _context.Workers.AnyAsync(w => w.WorkerId == workerId);
                if (!workerExists) return NotFound(new { message = "Worker not found." });

                var habits = await _context.WorkerHabits
                    .Where(wh => wh.WorkerId == workerId && wh.Habit != null && wh.Habit.IsActive)
                    .OrderBy(wh => wh.Habit!.SortOrder)
                    .ThenBy(wh => wh.Habit!.Name)
                    .Select(wh => new
                    {
                        id = wh.Habit!.HabitId,
                        name = wh.Habit!.Name
                    })
                    .ToListAsync();

                return Ok(new
                {
                    workerId = workerId,
                    habitIds = habits.Select(h => h.id).ToList(),
                    habits = habits
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching worker habits: " + ex.Message });
            }
        }

        // ── Replace the worker's selection (signup sends its picks through
        //    AccountCreation/SignupWorker instead; this is for My Habits / Edit Profile) ──
        [HttpPost("UpdateWorkerHabits")]
        public async Task<IActionResult> UpdateWorkerHabits([FromBody] WorkerHabitsDto model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (model == null || model.WorkerId <= 0)
                    return BadRequest(new { message = "Worker id is required." });

                var worker = await _context.Workers.FindAsync(model.WorkerId);
                if (worker == null) return NotFound(new { message = "Worker not found." });

                var wanted = (model.HabitIds ?? new List<int>())
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                // Only ids that really exist in the master list are accepted.
                var validIds = await _context.Habits
                    .Where(h => wanted.Contains(h.HabitId))
                    .Select(h => h.HabitId)
                    .ToListAsync();

                var existing = await _context.WorkerHabits
                    .Where(wh => wh.WorkerId == model.WorkerId)
                    .ToListAsync();

                var toRemove = existing.Where(wh => !validIds.Contains(wh.HabitId)).ToList();
                if (toRemove.Any()) _context.WorkerHabits.RemoveRange(toRemove);

                var alreadyThere = existing.Select(wh => wh.HabitId).ToHashSet();
                var toAdd = validIds
                    .Where(id => !alreadyThere.Contains(id))
                    .Select(id => new WorkerHabits
                    {
                        WorkerId = model.WorkerId,
                        HabitId = id,
                        CreatedDate = DateTime.Now
                    })
                    .ToList();

                if (toAdd.Any()) _context.WorkerHabits.AddRange(toAdd);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var saved = await _context.WorkerHabits
                    .Where(wh => wh.WorkerId == model.WorkerId && wh.Habit != null)
                    .OrderBy(wh => wh.Habit!.SortOrder)
                    .Select(wh => new { id = wh.Habit!.HabitId, name = wh.Habit!.Name })
                    .ToListAsync();

                return Ok(new
                {
                    message = saved.Count == 1
                        ? "Habit updated."
                        : $"Habits updated ({saved.Count} selected).",
                    workerId = model.WorkerId,
                    habitIds = saved.Select(h => h.id).ToList(),
                    habits = saved
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error saving habits: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        public class WorkerHabitsDto
        {
            public int WorkerId { get; set; }
            public List<int>? HabitIds { get; set; }
        }
    }
}
