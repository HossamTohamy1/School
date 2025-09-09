using MediatR;
using School_managment.Features.Teachers.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Teachers.Commands
{
    public class UpdateTeacherCommand : IRequest<ResponseViewModel<TeacherDto>>
    {
        public UpdateTeacherDto Teacher { get; set; }

        public UpdateTeacherCommand(UpdateTeacherDto teacher)
        {
            Teacher = teacher;
        }
    }
}
