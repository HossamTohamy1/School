using AutoMapper;
using MediatR;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Models;
using School_managment.Features.Classes.Queries;
using School_managment.Infrastructure.Interface;
using School_managment.Common.Enums;
using School_managment.ViewModels;

namespace School_managment.Features.Classes.Handlers
{
    public class GetClassByIdHandler : IRequestHandler<GetClassByIdQuery, ResponseViewModel<ClassDto>>
    {
        private readonly IRepository<Class> _repository;
        private readonly IMapper _mapper;

        public GetClassByIdHandler(IRepository<Class> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ResponseViewModel<ClassDto>> Handle(GetClassByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await _repository.GetByIdAsyncWithTracking(request.Id);
                if (entity == null)
                {
                    return new ErrorResponseViewModel<ClassDto>("Class not found", ErrorCode.NotFound);
                }

                var dto = _mapper.Map<ClassDto>(entity);
                return new SuccessResponseViewModel<ClassDto>(dto, "Class fetched successfully.");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<ClassDto>($"Error fetching class: {ex.Message}", ErrorCode.InternalError);
            }
        }
    }
}
