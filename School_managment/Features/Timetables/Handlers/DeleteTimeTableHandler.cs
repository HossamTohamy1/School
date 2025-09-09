using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Enums;
using School_managment.Features.Timetables.Commands;
using School_managment.Infrastructure;
using School_managment.ViewModels;

public class DeleteTimeTableHandler : IRequestHandler<DeleteTimeTableCommand, ResponseViewModel<bool>>
{
    private readonly SchoolDbContext _context;

    public DeleteTimeTableHandler(SchoolDbContext context)
    {
        _context = context;
    }

    public async Task<ResponseViewModel<bool>> Handle(DeleteTimeTableCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var timeTable = await _context.Timetables
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (timeTable == null)
            {
                return new ErrorResponseViewModel<bool>(
                    $"TimeTable with ID {request.Id} not found.",
                    ErrorCode.NotFound);
            }

            var slots = await _context.TimetableSlots
                .Where(ts => ts.TimetableId == request.Id)
                .ToListAsync(cancellationToken);

            _context.TimetableSlots.RemoveRange(slots);
            _context.Timetables.Remove(timeTable);

            await _context.SaveChangesAsync(cancellationToken);

            return new SuccessResponseViewModel<bool>(
                true,
                "Timetable deleted successfully");
        }
        catch (Exception ex)
        {
            return new ErrorResponseViewModel<bool>(
                $"Error deleting timetable: {ex.Message}",
                ErrorCode.InternalError);
        }
    }
}
