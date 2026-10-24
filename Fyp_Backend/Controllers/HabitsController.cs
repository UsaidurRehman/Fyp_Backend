using Fyp_Backend.Models;
using Fyp_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fyp_Backend.Controllers
{
    // Worker-selectable habits, client-search filters, and a worker's selections.
    //
    //  GET  api/Habits/GetHabits                        -> worker checklist
    //  GET  api/Habits/GetClientFilterHabits            -> client filter options
    //  GET  api/Habits/GetWorkerHabits/{workerId}       -> that worker's selection
    //  POST api/Habits/UpdateWorkerHabits               -> replace the worker's selection
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

        // Worker signup / My Habits list. This intentionally does not return
        // every master row once the table is configured by audience.
        [HttpGet("GetHabits")]
        [AllowAnonymous] // signup calls this before a worker has a token
        public async Task<IActionResult> GetHabits()
        {
            try
            {
                var habits = await _context.Habits
                    .Where(h => h.IsActive && h.IsWorkerSelectable)
                    .OrderBy(h => h.SortOrder)
                    .ThenBy(h => h.Name)
                    .Select(h => new
                    {
                        h.HabitId,
                        h.Name,
                        h.SortOrder,
                        h.ExclusiveGroup
                    })
                    .ToListAsync();

                return Ok(habits);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching worker habits: " + ex.Message });
            }
        }

        // Only habits explicitly marked as useful search criteria are returned.
        [HttpGet("GetClientFilterHabits")]
        [AllowAnonymous]
        public async Task<IActionResult> GetClientFilterHabits()
        {
            try
            {
                var habits = await _context.Habits
                    .Where(h => h.IsActive && h.IsWorkerSelectable && h.IsClientFilterable)
                    .OrderBy(h => h.SortOrder)
                    .ThenBy(h => h.Name)
                    .Select(h => new
                    {
                        h.HabitId,
                        h.Name,
                        h.SortOrder,
                        h.ExclusiveGroup
                    })
                    .ToListAsync();

                return Ok(habits);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching client habit filters: " + ex.Message });
            }
        }

        // What one worker has selected.
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

        // Replace the worker's selection. Signup and profile-edit use their
        // respective account endpoints; this endpoint is used by My Habits.
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

                var requestedHabits = await _context.Habits
                    .Where(h => wanted.Contains(h.HabitId))
                    .ToListAsync();
                var selectableHabits = requestedHabits
                    .Where(h => h.IsActive && h.IsWorkerSelectable)
                    .ToList();

                if (selectableHabits.Count != wanted.Count)
                    return BadRequest(new { message = "One of the selected habits is inactive or is not available for worker profiles." });

                if (HabitSelectionRules.FindExclusiveGroupConflict(selectableHabits) != null)
                    return BadRequest(new { message = "Choose only one option from each opposite habit pair." });

                var validIds = selectableHabits.Select(h => h.HabitId).ToList();
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
