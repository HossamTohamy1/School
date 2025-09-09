using MediatR;
using School_managment.ViewModels;

namespace School_managment.Features.Classes.Commands
{
 
    public record DeleteClassCommand(int Id) : IRequest<ResponseViewModel<bool>>;

}
