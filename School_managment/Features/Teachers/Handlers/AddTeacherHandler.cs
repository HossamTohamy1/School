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

public class AddTeacherHandler : IRequestHandler<AddTeacherCommand, ResponseViewModel<TeacherDto>>
{
    private readonly IRepository<Teacher> _teacherRepository;
    private readonly IMapper _mapper;

    public AddTeacherHandler(IRepository<Teacher> teacherRepository, IMapper mapper)
    {
        _teacherRepository = teacherRepository;
        _mapper = mapper;
    }

    public async Task<ResponseViewModel<TeacherDto>> Handle(AddTeacherCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var teacher = _mapper.Map<Teacher>(request.Teacher);

            await _teacherRepository.AddAsync(teacher);

            var dto = await _teacherRepository.GetAll()
                        .Where(t => t.Id == teacher.Id)
                        .ProjectTo<TeacherDto>(_mapper.ConfigurationProvider)
                        .FirstOrDefaultAsync(cancellationToken);

            return new SuccessResponseViewModel<TeacherDto>(
                dto!,
                "Teacher added successfully");
        }
        catch (Exception ex)
        {
            return new ErrorResponseViewModel<TeacherDto>(
                $"Error adding teacher: {ex.Message}",
                ErrorCode.InternalError);
        }
    }
}
