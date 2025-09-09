using MediatR;
using School_managment.Common.Models;
using School_managment.Features.Teachers.Commands;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Queries;
using School_managment.ViewModels;

namespace School_managment.Features.Teachers.Orchestrators
{
    public class TeacherOrchestrator
    {
        private readonly IMediator _mediator;

        public TeacherOrchestrator(IMediator mediator)
        {
            _mediator = mediator;
        }

        public Task<ResponseViewModel<TeacherDto>> AddTeacherAsync(AddTeacherDto dto) =>
            _mediator.Send(new AddTeacherCommand(dto));

        public Task<ResponseViewModel<TeacherDto>> UpdateTeacherAsync(UpdateTeacherDto dto) =>
            _mediator.Send(new UpdateTeacherCommand(dto));

        public Task<ResponseViewModel<bool>> DeleteTeacherAsync(int id) =>
            _mediator.Send(new DeleteTeacherCommand(id));

        public Task<ResponseViewModel<List<TeacherDto>>> GetAllTeachersAsync() =>
            _mediator.Send(new GetAllTeachersQuery());

        public Task<ResponseViewModel<TeacherDto>> GetTeacherByIdAsync(int id) =>
            _mediator.Send(new GetTeacherByIdQuery(id));


        public Task<ResponseViewModel<PagedResult<TeacherDto>>> GetTeachersAsync(int pageNumber, int pageSize, string? searchTerm = null) =>
            _mediator.Send(new GetTeachersQuery(pageNumber, pageSize, searchTerm));
    }
}
