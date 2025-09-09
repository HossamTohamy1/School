using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Enums;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;
using School_managment.Features.Teachers.Queries;
using School_managment.Infrastructure.Interface;
using School_managment.ViewModels;

namespace School_managment.Features.Teachers.Handlers
{
    public class GetTeacherByIdHandler : IRequestHandler<GetTeacherByIdQuery, ResponseViewModel<TeacherDto>>
    {
        private readonly IRepository<Teacher> _repository;
        private readonly IMapper _mapper;

        public GetTeacherByIdHandler(IRepository<Teacher> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ResponseViewModel<TeacherDto>> Handle(GetTeacherByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var dto = await _repository.GetAll()
                    .Where(t => t.Id == request.Id)
                    .ProjectTo<TeacherDto>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(cancellationToken);

                if (dto == null)
                    return new ErrorResponseViewModel<TeacherDto>("Teacher not found", ErrorCode.NotFound);

                return new SuccessResponseViewModel<TeacherDto>(dto, "Teacher fetched successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<TeacherDto>(
                    $"Error fetching teacher: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }
    }
}
