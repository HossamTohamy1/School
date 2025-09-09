using MediatR;
using School_managment.Common.Models;
using School_managment.Features.Classes.Commands;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Queries;
using School_managment.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace School_managment.Features.Classes.Orchestrators
{
    public class ClassOrchestrator
    {
        private readonly IMediator _mediator;

        public ClassOrchestrator(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<ResponseViewModel<ClassDto>> AddClassAsync(AddClassDto dto)
        {
            return await _mediator.Send(new AddClassCommand(dto));
        }

        public async Task<ResponseViewModel<ClassDto>> UpdateClassAsync(UpdateClassDto dto)
        {
            return await _mediator.Send(new UpdateClassCommand(dto));
        }

        public async Task<ResponseViewModel<bool>> DeleteClassAsync(int id)
        {
            return await _mediator.Send(new DeleteClassCommand(id));
        }

        public async Task<ResponseViewModel<List<ClassDto>>> GetAllClassesAsync()
        {
            return await _mediator.Send(new GetAllClassesQuery());
        }

        public async Task<ResponseViewModel<PagedResult<ClassDto>>> GetAllClassesPageNumberAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null)
        {
            return await _mediator.Send(new GetAllClassesPageNumberQuery(pageNumber, pageSize, searchTerm));
        }

        public async Task<ResponseViewModel<ClassDto>> GetClassByIdAsync(int id)
        {
            return await _mediator.Send(new GetClassByIdQuery(id));
        }
    }
}
