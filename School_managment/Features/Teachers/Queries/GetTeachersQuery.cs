using MediatR;
using School_managment.Common.Models;
using School_managment.Features.Teachers.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Teachers.Queries
{
    public class GetTeachersQuery : IRequest<ResponseViewModel<PagedResult<TeacherDto>>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }

        public GetTeachersQuery(int pageNumber, int pageSize, string? searchTerm = null)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            SearchTerm = searchTerm;
        }
    }
}
