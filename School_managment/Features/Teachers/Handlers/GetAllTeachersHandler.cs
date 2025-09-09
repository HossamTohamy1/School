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
    public class GetAllTeachersHandler : IRequestHandler<GetAllTeachersQuery, ResponseViewModel<List<TeacherDto>>>
    {
        private readonly IRepository<Teacher> _repository;
        private readonly IMapper _mapper;

        public GetAllTeachersHandler(IRepository<Teacher> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ResponseViewModel<List<TeacherDto>>> Handle(GetAllTeachersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var teachers = await _repository.GetAll()
                    .ProjectTo<TeacherDto>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                return new SuccessResponseViewModel<List<TeacherDto>>(teachers, "Teachers fetched successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<List<TeacherDto>>(
                    $"Error fetching teachers: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }
    }
}
