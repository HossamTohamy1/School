using MediatR;
using School_managment.Features.Timetables.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Timetables.Queries
{
    public class GetByIdTimeTableQuery : IRequest<ResponseViewModel<TimeTableDto>>
    {
        public int Id { get; set; }

        public GetByIdTimeTableQuery(int id)
        {
            Id = id;
        }
    }
}
