using MediatR;
using School_managment.Features.Timetables.DTOs;
using School_managment.ViewModels;

public class GetAllTimeTableQuery : IRequest<ResponseViewModel<List<TimeTableDto>>>
{
    public int? ClassId { get; set; }
    public bool? IsActive { get; set; }

    public GetAllTimeTableQuery(int? classId = null, bool? isActive = null)
    {
        ClassId = classId;
        IsActive = isActive;
    }
}
