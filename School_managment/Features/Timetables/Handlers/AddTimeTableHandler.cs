using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.Commands;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Models;
using School_managment.Infrastructure;
using School_managment.ViewModels;
using School_managment.Common.Enums;

namespace School_managment.Features.Timetables.Handlers
{
    public class AddTimeTableHandler : IRequestHandler<AddTimeTableCommand, ResponseViewModel<TimeTableDto>>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public AddTimeTableHandler(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ResponseViewModel<TimeTableDto>> Handle(AddTimeTableCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var classId = request.TimeTable.ClassId;

                var classExists = await _context.Classes.AnyAsync(c => c.Id == classId, cancellationToken);
                if (!classExists)
                {
                    return new ErrorResponseViewModel<TimeTableDto>(
                        $"Class with ID {classId} does not exist.",
                        ErrorCode.NotFound);
                }

                var existingActiveTimetables = await _context.Timetables
                    .Where(t => t.ClassId == classId && t.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var timetable in existingActiveTimetables)
                {
                    timetable.IsActive = false;
                }

                var timeTable = new TimeTable
                {
                    ClassId = classId,
                    ScheduleId = request.TimeTable.ScheduleId,
                    GeneratedAt = DateTime.UtcNow,
                    IsActive = true,
                    Constraints = request.TimeTable.Constraints ?? "{}"
                };

                _context.Timetables.Add(timeTable);
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var slotDto in request.TimeTable.TimetableSlots)
                {
                    var slot = new TimetableSlot
                    {
                        TimetableId = timeTable.Id,
                        ClassId = timeTable.ClassId,
                        Period = slotDto.Period,
                        DayOfWeek = slotDto.DayOfWeek,
                        SubjectId = slotDto.SubjectId,
                        TeacherId = slotDto.TeacherId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.TimetableSlots.Add(slot);
                }

                await _context.SaveChangesAsync(cancellationToken);

                var createdTimeTable = await GetTimetableWithRelatedDataAsync(timeTable.Id, cancellationToken);

                return new SuccessResponseViewModel<TimeTableDto>(
                    _mapper.Map<TimeTableDto>(createdTimeTable),
                    "Timetable created successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<TimeTableDto>(
                    $"Error creating timetable: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

        private async Task<TimeTable> GetTimetableWithRelatedDataAsync(int timetableId, CancellationToken cancellationToken)
        {
            var timetable = await _context.Timetables
                .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);

            if (timetable != null)
            {
                timetable.Class = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Id == timetable.ClassId, cancellationToken);

                var slots = await _context.TimetableSlots
                    .Where(ts => ts.TimetableId == timetableId)
                    .ToListAsync(cancellationToken);

                foreach (var slot in slots)
                {
                    if (slot.SubjectId.HasValue)
                    {
                        slot.Subject = await _context.Subjects
                            .FirstOrDefaultAsync(s => s.Id == slot.SubjectId.Value, cancellationToken);
                    }

                    if (slot.TeacherId.HasValue && slot.TeacherId.Value != 0)
                    {
                        slot.Teacher = await _context.Teachers
                            .FirstOrDefaultAsync(t => t.Id == slot.TeacherId.Value, cancellationToken);
                    }
                }

                timetable.TimetableSlots = slots;
            }

            return timetable;
        }
    }
}