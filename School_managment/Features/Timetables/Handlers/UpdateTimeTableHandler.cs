using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Enums;
using School_managment.Features.Timetables.Commands;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Models;
using School_managment.Infrastructure;
using School_managment.ViewModels;

public class UpdateTimeTableHandler : IRequestHandler<UpdateTimeTableCommand, ResponseViewModel<TimeTableDto>>
{
    private readonly SchoolDbContext _context;
    private readonly IMapper _mapper;

    public UpdateTimeTableHandler(SchoolDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ResponseViewModel<TimeTableDto>> Handle(UpdateTimeTableCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var timeTable = await _context.Timetables
                .FirstOrDefaultAsync(t => t.Id == request.TimeTable.Id, cancellationToken);

            if (timeTable == null)
            {
                return new ErrorResponseViewModel<TimeTableDto>(
                    $"TimeTable with ID {request.TimeTable.Id} not found.",
                    ErrorCode.NotFound);
            }

            var classExists = await _context.Classes.AnyAsync(c => c.Id == request.TimeTable.ClassId, cancellationToken);
            if (!classExists)
            {
                return new ErrorResponseViewModel<TimeTableDto>(
                    $"Class with ID {request.TimeTable.ClassId} does not exist.",
                    ErrorCode.NotFound);
            }

            timeTable.ClassId = request.TimeTable.ClassId;
            timeTable.ScheduleId = request.TimeTable.ScheduleId;
            timeTable.IsActive = request.TimeTable.IsActive;
            timeTable.Constraints = request.TimeTable.Constraints ?? "{}";

            var existingSlots = await _context.TimetableSlots
                .Where(ts => ts.TimetableId == timeTable.Id)
                .ToListAsync(cancellationToken);

            _context.TimetableSlots.RemoveRange(existingSlots);

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

            var updatedTimeTable = await GetTimetableWithRelatedDataAsync(timeTable, cancellationToken);

            return new SuccessResponseViewModel<TimeTableDto>(
                _mapper.Map<TimeTableDto>(updatedTimeTable),
                "Timetable updated successfully");
        }
        catch (Exception ex)
        {
            return new ErrorResponseViewModel<TimeTableDto>(
                $"Error updating timetable: {ex.Message}",
                ErrorCode.InternalError);
        }
    }

    private async Task<TimeTable> GetTimetableWithRelatedDataAsync(TimeTable timetable, CancellationToken cancellationToken)
    {
        timetable.Class = await _context.Classes
            .FirstOrDefaultAsync(c => c.Id == timetable.ClassId, cancellationToken);

        var slots = await _context.TimetableSlots
            .Where(ts => ts.TimetableId == timetable.Id)
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
        return timetable;
    }
}