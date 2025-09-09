using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Enums;
using School_managment.Features.Teachers.Commands;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;
using School_managment.Infrastructure.Interface;
using School_managment.ViewModels;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace School_managment.Features.Teachers.Handlers
{
    public class UpdateTeacherHandler : IRequestHandler<UpdateTeacherCommand, ResponseViewModel<TeacherDto>>
    {
        private readonly IRepository<Teacher> _teacherRepository;
        private readonly IMapper _mapper;

        public UpdateTeacherHandler(IRepository<Teacher> teacherRepository, IMapper mapper)
        {
            _teacherRepository = teacherRepository;
            _mapper = mapper;
        }

        public async Task<ResponseViewModel<TeacherDto>> Handle(UpdateTeacherCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingTeacher = await _teacherRepository.GetAll()
                    .FirstOrDefaultAsync(t => t.Id == request.Teacher.Id, cancellationToken);

                if (existingTeacher == null)
                {
                    return new ErrorResponseViewModel<TeacherDto>(
                        "Teacher not found",
                        ErrorCode.NotFound);
                }

                _mapper.Map(request.Teacher, existingTeacher);

                await _teacherRepository.UpdateAsync(existingTeacher);

                var dto = await _teacherRepository.GetAll()
                    .Where(t => t.Id == existingTeacher.Id)
                    .ProjectTo<TeacherDto>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(cancellationToken);

                return new SuccessResponseViewModel<TeacherDto>(
                    dto!,
                    "Teacher updated successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<TeacherDto>(
                    $"Error updating teacher: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }
    }
}
