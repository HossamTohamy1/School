using MediatR;
using School_managment.Features.Classes.Commands;
using School_managment.Features.Classes.Models;
using School_managment.Infrastructure.Interface;
using School_managment.Common.Enums;
using School_managment.ViewModels;

namespace School_managment.Features.Classes.Handlers
{
    public class DeleteClassHandler : IRequestHandler<DeleteClassCommand, ResponseViewModel<bool>>
    {
        private readonly IRepository<Class> _repository;

        public DeleteClassHandler(IRepository<Class> repository)
        {
            _repository = repository;
        }

        public async Task<ResponseViewModel<bool>> Handle(DeleteClassCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(request.Id);
                if (entity == null)
                {
                    return new ErrorResponseViewModel<bool>("Class not found", ErrorCode.NotFound);
                }

                await _repository.DeleteAsync(entity);

                return new SuccessResponseViewModel<bool>(true, "Class deleted successfully.");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<bool>($"Error deleting class: {ex.Message}", ErrorCode.InternalError);
            }
        }
    }
}
