using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Enums;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Models;
using School_managment.Features.Timetables.Queries;
using School_managment.Infrastructure;
using School_managment.ViewModels;

public class GetAllTimeTableHandler : IRequestHandler<GetAllTimeTableQuery, ResponseViewModel<List<TimeTableDto>>>
{
    private readonly SchoolDbContext _context;
    private readonly IMapper _mapper;

    public GetAllTimeTableHandler(SchoolDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ResponseViewModel<List<TimeTableDto>>> Handle(GetAllTimeTableQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Timetables.AsQueryable();

            if (request.ClassId.HasValue)
            {
                query = query.Where(t => t.ClassId == request.ClassId.Value);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == request.IsActive.Value);
            }

            var timetables = await query
                .OrderByDescending(t => t.GeneratedAt)
                .ToListAsync(cancellationToken);

            var timetableDtos = new List<TimeTableDto>();

            foreach (var timetable in timetables)
            {
                var enrichedTimetable = await GetTimetableWithRelatedDataAsync(timetable, cancellationToken);
                timetableDtos.Add(_mapper.Map<TimeTableDto>(enrichedTimetable));
            }

            return new SuccessResponseViewModel<List<TimeTableDto>>(
                timetableDtos,
                $"Retrieved {timetableDtos.Count} timetables successfully");
        }
        catch (Exception ex)
        {
            return new ErrorResponseViewModel<List<TimeTableDto>>(
                $"Error retrieving timetables: {ex.Message}",
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
