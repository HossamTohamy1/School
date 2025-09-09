using MediatR;
using School_managment.Features.Classes.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Classes.Queries
{
    public class GetAllClassesQuery : IRequest<ResponseViewModel<List<ClassDto>>>
    {
    }
}
