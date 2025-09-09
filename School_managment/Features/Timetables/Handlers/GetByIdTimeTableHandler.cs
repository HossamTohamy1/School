using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Enums;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Models;
using School_managment.Features.Timetables.Queries;
using School_managment.Infrastructure;
using School_managment.ViewModels;

public class GetByIdTimeTableHandler : IRequestHandler<GetByIdTimeTableQuery, ResponseViewModel<TimeTableDto>>
{
    private readonly SchoolDbContext _context;
    private readonly IMapper _mapper;

    public GetByIdTimeTableHandler(SchoolDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ResponseViewModel<TimeTableDto>> Handle(GetByIdTimeTableQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var timeTable = await _context.Timetables
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (timeTable == null)
            {
                return new ErrorResponseViewModel<TimeTableDto>(
                    $"TimeTable with ID {request.Id} not found.",
                    ErrorCode.NotFound);
            }

            var enrichedTimetable = await GetTimetableWithRelatedDataAsync(timeTable, cancellationToken);

            return new SuccessResponseViewModel<TimeTableDto>(
                _mapper.Map<TimeTableDto>(enrichedTimetable),
                "Timetable retrieved successfully");
        }
        catch (Exception ex)
        {
            return new ErrorResponseViewModel<TimeTableDto>(
                $"Error retrieving timetable: {ex.Message}",
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