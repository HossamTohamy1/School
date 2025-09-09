using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.Commands;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Models;
using School_managment.Features.Timetables.Orchestrators;
using School_managment.Features.Timetables.Queries;
using School_managment.Infrastructure;

namespace School_managment.Features.Timetables.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeTableController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ITimeTableOrchestrator _orchestrator;
        private readonly SchoolDbContext _context;

        public TimeTableController(IMediator mediator, ITimeTableOrchestrator orchestrator, SchoolDbContext context)
        {
            _mediator = mediator;
            _orchestrator = orchestrator;
            _context = context;
        }

        #region Basic CRUD Operations

    
        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] int? classId = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAllTimeTableQuery(classId, isActive);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var query = new GetByIdTimeTableQuery(id);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return NotFound(result.Message);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] AddTimeTableDto timeTable)
        {
            var command = new AddTimeTableCommand(timeTable);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result.Data);
        }

     
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateTimeTableDto timeTable)
        {
            if (id != timeTable.Id)
                return BadRequest("ID mismatch between route and body.");

            var command = new UpdateTimeTableCommand(timeTable);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var command = new DeleteTimeTableCommand(id);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return NotFound(result.Message);

            return NoContent();
        }

        #endregion

        #region Enhanced Smart Timetable Generation

       
        [HttpPost("generate-smart/{classId}")]
        public async Task<ActionResult> GenerateEnhancedSmartTimetable(
            int classId,
            [FromBody] SmartTimetableRequest request)
        {
            var suggestionsResponse = await _orchestrator.GetConflictPreventionSuggestionsAsync(classId);
            if (!suggestionsResponse.IsSuccess)
                return BadRequest(suggestionsResponse.Message);

            var suggestions = suggestionsResponse.Data;

            var criticalIssues = suggestions.Where(s => s.Priority == "High").ToList();
            if (criticalIssues.Any())
            {
                return BadRequest(new
                {
                    message = "Critical issues found that may affect timetable generation",
                    suggestions = criticalIssues,
                    canProceed = false
                });
            }

            var result = await _orchestrator.GenerateSmartTimetableAsync(classId, request);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    message = result.Message,
                    suggestions = suggestions.Where(s => s.Priority == "Medium").ToList()
                });
            }

            return Ok(result.Data);
        }

        #endregion

        #region Smart Assignment and Availability

        [HttpGet("available-teachers/{classId}")]
        public async Task<ActionResult> GetAvailableTeachersForSlot(
            int classId,
            [FromQuery] DayOfWeek dayOfWeek,
            [FromQuery] int period)
        {
            if (period < 1 || period > 8)
                return BadRequest("Period must be between 1 and 8");

            var result = await _orchestrator.GetAvailableTeachersForSlotAsync(classId, dayOfWeek, period);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }


        [HttpPost("assign-teacher-to-slot")]
        public async Task<ActionResult> AssignTeacherToSlot([FromBody] SlotAssignmentRequest request)
        {
            if (request.TeacherId <= 0 || request.ClassId <= 0 || request.SubjectId <= 0)
                return BadRequest("Invalid teacher, class, or subject ID");

            if (request.Period < 1 || request.Period > 8)
                return BadRequest("Period must be between 1 and 8");

            var result = await _orchestrator.AssignTeacherToSlotAsync(request);

            if (!result.IsSuccess)
            {
                if (result.Data?.ConflictDetails != null)
                {
                    return Conflict(result.Data); 
                }
                return BadRequest(result.Data);
            }

            return Ok(result.Data);
        }

        #endregion

        #region Conflict Prevention and Validation

        [HttpGet("class/{classId}/conflict-prevention-suggestions")]
        public async Task<ActionResult> GetConflictPreventionSuggestions(int classId)
        {
            var result = await _orchestrator.GetConflictPreventionSuggestionsAsync(classId);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("{id}/validate")]
        public async Task<ActionResult> ValidateTimetable(int id)
        {
            var result = await _orchestrator.ValidateTimetableAsync(id);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

    
        [HttpPost("check-teacher-conflict")]
        public async Task<ActionResult> CheckTeacherConflict([FromBody] CheckTeacherConflictDto dto)
        {
            if (dto.TeacherId <= 0)
                return BadRequest("Invalid teacher ID");

            if (dto.ClassId <= 0)
                return BadRequest("Invalid class ID");

            if (dto.Period < 1 || dto.Period > 8)
                return BadRequest("Period must be between 1 and 8");

            var result = await _orchestrator.CheckTeacherConflictAsync(
                dto.TeacherId,
                dto.DayOfWeek,
                dto.Period,
                dto.ClassId
            );

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        #endregion

        #region Conflict Resolution

    
        [HttpPost("{id}/resolve-conflicts")]
        public async Task<ActionResult> ResolveConflictsAutomatically(int id)
        {
            var timetableQuery = new GetByIdTimeTableQuery(id);
            var timetableResult = await _mediator.Send(timetableQuery);

            if (!timetableResult.IsSuccess)
                return NotFound($"Timetable with ID {id} not found");

            var conflictsResult = await _orchestrator.ValidateTimetableAsync(id);
            if (!conflictsResult.IsSuccess)
                return BadRequest(conflictsResult.Message);

            var conflicts = conflictsResult.Data;
            if (!conflicts.Any())
            {
                return Ok(new ConflictResolutionResult
                {
                    Success = true,
                    Message = "No conflicts found to resolve",
                    ResolvedCount = 0,
                    UnresolvedCount = 0
                });
            }

            var teacherConflicts = conflicts.Where(c =>
                c.Type == "TeacherDoubleBooking" ||
                c.Type == "TeacherCrossClassDoubleBooking"
            ).ToList();

            if (!teacherConflicts.Any())
            {
                return Ok(new ConflictResolutionResult
                {
                    Success = true,
                    Message = "No teacher conflicts found to resolve",
                    ResolvedCount = 0,
                    UnresolvedCount = 0
                });
            }

            var resolutionResult = await _orchestrator.ResolveTeacherConflictsAsync(id, teacherConflicts);

            if (!resolutionResult.IsSuccess)
                return BadRequest(resolutionResult.Message);

            return Ok(resolutionResult.Data);
        }

        #endregion

        #region Statistics and Quality Analysis

        [HttpGet("{id}/statistics")]
        public async Task<ActionResult> GetStatistics(int id)
        {
            var result = await _orchestrator.GetTimetableStatisticsAsync(id);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        #endregion

        #region Slot Management

        [HttpPut("{id}/swap-slots")]
        public async Task<ActionResult> SwapSlots(int id, [FromBody] SwapSlotsRequest request)
        {
            var result = await _orchestrator.SwapTimetableSlotsAsync(id, request);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(new { message = "Slots swapped successfully." });
        }

        #endregion

        #region Status Management

        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var timetableQuery = new GetByIdTimeTableQuery(id);
            var timetableResult = await _mediator.Send(timetableQuery);

            if (!timetableResult.IsSuccess)
                return NotFound($"TimeTable with ID {id} not found.");

            var timetable = timetableResult.Data;

            var updateDto = new UpdateTimeTableDto
            {
                Id = timetable.Id,
                ClassId = timetable.ClassId,
                ScheduleId = timetable.ScheduleId,
                IsActive = request.IsActive,
                Constraints = timetable.Constraints,
                TimetableSlots = timetable.TimetableSlots.Select(ts => new UpdateTimetableSlotDto
                {
                    Id = ts.Id,
                    Period = ts.Period,
                    DayOfWeek = ts.DayOfWeek,
                    SubjectId = ts.SubjectId,
                    TeacherId = ts.TeacherId
                }).ToList()
            };

            var command = new UpdateTimeTableCommand(updateDto);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        #endregion

        #region Class-Specific Operations


        [HttpGet("class/{classId}")]
        public async Task<ActionResult> GetByClass(int classId)
        {
            var query = new GetAllTimeTableQuery(classId);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("class/{classId}/active")]
        public async Task<ActionResult> GetActiveByClass(int classId)
        {
            var query = new GetAllTimeTableQuery(classId, true);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            var activeTimetable = result.Data.FirstOrDefault();

            if (activeTimetable == null)
                return NotFound($"No active timetable found for class with ID {classId}.");

            return Ok(activeTimetable);
        }

        [HttpGet("class/{classId}/assigned-subjects")]
        public async Task<ActionResult<List<ClassSubjectWithTeacherDto>>> GetAssignedSubjectsForClass(int classId)
        {
            try
            {
                var classExists = await _context.Classes.AnyAsync(c => c.Id == classId);
                if (!classExists)
                {
                    return NotFound($"Class with ID {classId} not found.");
                }

                var classSubjects = await _context.ClassTeachers
                    .Include(cs => cs.Subject)
                    .Include(cs => cs.Teacher)
                    .Where(cs => cs.ClassId == classId)
                    .Select(cs => new ClassSubjectWithTeacherDto
                    {
                        SubjectId = cs.SubjectId,
                        SubjectName = cs.Subject.Name,
                        TeacherId = cs.TeacherId,
                        TeacherName = cs.Teacher != null ? cs.Teacher.Name : "No Teacher Assigned",
                        HoursPerWeek = cs.Subject.HoursPerWeek,
                        IsAssigned = cs.TeacherId != null,
                        SubjectColor = GenerateSubjectColor(cs.Subject.Name)
                    })
                    .OrderBy(cs => cs.SubjectName)
                    .ToListAsync();

                return Ok(classSubjects);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving class subjects: {ex.Message}");
            }
        }

        #endregion

        #region Direct Database Operations (Legacy Support)

        [HttpDelete("slots/{slotId}")]
        public async Task<IActionResult> DeleteSlot(int slotId)
        {
            var slot = await _context.TimetableSlots.FindAsync(slotId);
            if (slot == null)
                return NotFound();

            _context.TimetableSlots.Remove(slot);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("add-slot")]
        public async Task<IActionResult> AddSlot([FromBody] AddTimetableSlotForAddSlotsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var slot = new TimetableSlot
            {
                ClassId = dto.ClassId,
                TimetableId = dto.TimetableId,
                Period = dto.Period,
                DayOfWeek = dto.DayOfWeek,
                SubjectId = dto.SubjectId,
                TeacherId = dto.TeacherId
            };

            _context.TimetableSlots.Add(slot);
            await _context.SaveChangesAsync();

            return Ok(slot);
        }

        #endregion

        #region Helper Methods


        private string GenerateSubjectColor(string subjectName)
        {
            var colors = new[]
            {
                "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD",
                "#FF9F43", "#6C5CE7", "#FD79A8", "#00B894", "#0984E3", "#00CEC9",
                "#E84393", "#FDCB6E", "#A29BFE", "#55A3FF"
            };

            int hash = 0;
            foreach (char c in subjectName)
            {
                hash = (hash * 31 + c) % int.MaxValue;
            }

            return colors[Math.Abs(hash) % colors.Length];
        }

        #endregion
    }
}