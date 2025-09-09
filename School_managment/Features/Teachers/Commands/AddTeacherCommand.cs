using MediatR;
using School_managment.Features.Teachers.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Teachers.Commands
{
    public class AddTeacherCommand : IRequest<ResponseViewModel<TeacherDto>>
    {
        public AddTeacherDto Teacher { get; set; }

        public AddTeacherCommand(AddTeacherDto teacher)
        {
            Teacher = teacher;
        }
    }
}
