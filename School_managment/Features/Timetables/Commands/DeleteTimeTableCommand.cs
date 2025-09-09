using MediatR;
using School_managment.ViewModels;

namespace School_managment.Features.Timetables.Commands
{
    public class DeleteTimeTableCommand : IRequest<ResponseViewModel<bool>>
    {
        public int Id { get; set; }

        public DeleteTimeTableCommand(int id)
        {
            Id = id;
        }
    }
}
