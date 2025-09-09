using MediatR;
using School_managment.ViewModels;

namespace School_managment.Features.Teachers.Commands
{
    public class DeleteTeacherCommand : IRequest<ResponseViewModel<bool>>
    {
        public int Id { get; init; }

        public DeleteTeacherCommand(int id)
        {
            Id = id;
        }
    }
}
