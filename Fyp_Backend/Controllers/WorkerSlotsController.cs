using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fyp_Backend.Models;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Fyp_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkerSlotsController : ControllerBase
    {
        private readonly Fyp1Context _context;

        public WorkerSlotsController(Fyp1Context context)
        {
            _context = context;
        }

        [HttpGet("GetWorkerTimeSlots/{workerId}")]
        public async Task<IActionResult> GetWorkerTimeSlots(int workerId)
        {
            try
            {
                var slots = await _context.WorkerTimeSlots
                    .Where(s => s.WorkerId == workerId)
                    .Select(s => new
                    {
                        s.Id,
                        s.WorkerId,
                        StartTime = DateTime.Today.Add(s.StartTime).ToString("hh:mm tt"),
                        EndTime = DateTime.Today.Add(s.EndTime).ToString("hh:mm tt"),
                        RawStartTime = s.StartTime.ToString(@"hh\:mm"),
                        RawEndTime = s.EndTime.ToString(@"hh\:mm")
                    })
                    .ToListAsync();

                return Ok(slots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving time slots: " + ex.Message });
            }
        }

        [HttpPost("AddTimeSlot")]
        public async Task<IActionResult> AddTimeSlot([FromBody] WorkerTimeSlots slot)
        {
            try
            {
                if (slot == null || slot.WorkerId <= 0)
                {
                    return BadRequest(new { message = "Invalid time slot data." });
                }

                if (slot.StartTime >= slot.EndTime)
                {
                    return BadRequest(new { message = "Start time must be before end time." });
                }

                // Check for overlapping slots
                bool isOverlapping = await _context.WorkerTimeSlots.AnyAsync(s => 
                    s.WorkerId == slot.WorkerId &&
                    (
                        (slot.StartTime >= s.StartTime && slot.StartTime < s.EndTime) ||
                        (slot.EndTime > s.StartTime && slot.EndTime <= s.EndTime) ||
                        (slot.StartTime <= s.StartTime && slot.EndTime >= s.EndTime)
                    )
                );

                if (isOverlapping)
                {
                    return BadRequest(new { message = "The selected time slot overlaps with an existing schedule." });
                }

                _context.WorkerTimeSlots.Add(slot);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Time slot added successfully!", slotId = slot.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding time slot: " + ex.Message });
            }
        }

        [HttpDelete("DeleteTimeSlot/{id}")]
        public async Task<IActionResult> DeleteTimeSlot(int id)
        {
            try
            {
                var slot = await _context.WorkerTimeSlots.FindAsync(id);
                if (slot == null)
                {
                    return NotFound(new { message = "Time slot not found." });
                }

                _context.WorkerTimeSlots.Remove(slot);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Time slot deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting time slot: " + ex.Message });
            }
        }
    }
}
