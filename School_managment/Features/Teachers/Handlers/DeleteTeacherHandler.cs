using MediatR;
using School_managment.Features.Teachers.Commands;
using School_managment.Features.Teachers.Models;
using School_managment.Features.Classes.Models;
using School_managment.Infrastructure.Interface;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using School_managment.Common.Enums;
using School_managment.ViewModels;
using School_managment.Common.Models;

namespace School_managment.Features.Teachers.Handlers
{
    public class DeleteTeacherHandler : IRequestHandler<DeleteTeacherCommand, ResponseViewModel<bool>>
    {
        private readonly IRepository<Teacher> _teacherRepository;
        private readonly IRepository<ClassTeacher> _classTeacherRepository;

        public DeleteTeacherHandler(
            IRepository<Teacher> teacherRepository,
            IRepository<ClassTeacher> classTeacherRepository)
        {
            _teacherRepository = teacherRepository;
            _classTeacherRepository = classTeacherRepository;
        }

        public async Task<ResponseViewModel<bool>> Handle(DeleteTeacherCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingTeacher = await _teacherRepository.GetByIdAsync(request.Id);
                if (existingTeacher == null)
                {
                    return new ErrorResponseViewModel<bool>("Teacher not found", ErrorCode.NotFound);
                }

                existingTeacher.IsDeleted = true;
                await _teacherRepository.UpdateAsync(existingTeacher);

                var relatedClassTeachers = _classTeacherRepository
                    .GetAll()
                    .Where(ct => ct.TeacherId == request.Id)
                    .ToList();

                foreach (var ct in relatedClassTeachers)
                {
                    await _classTeacherRepository.HardDeleteAsync(ct);
                }

                return new SuccessResponseViewModel<bool>(true, "Teacher deleted successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<bool>(
                    $"Error deleting teacher: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }
    }
}
