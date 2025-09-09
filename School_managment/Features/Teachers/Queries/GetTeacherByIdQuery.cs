using MediatR;
using School_managment.Features.Teachers.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Teachers.Queries
{
    public class GetTeacherByIdQuery : IRequest<ResponseViewModel<TeacherDto>>
    {
        public int Id { get; }

        public GetTeacherByIdQuery(int id)
        {
            Id = id;
        }
    }
}
