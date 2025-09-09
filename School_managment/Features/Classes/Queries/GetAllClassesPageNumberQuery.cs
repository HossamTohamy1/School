// Features/Classes/Queries/GetAllClassesPageNumberQuery.cs
using MediatR;
using School_managment.Common.Models;
using School_managment.Features.Classes.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Classes.Queries
{
    public class GetAllClassesPageNumberQuery : IRequest<ResponseViewModel<PagedResult<ClassDto>>>
    {
        public int PageNumber { get; }
        public int PageSize { get; }
        public string? SearchTerm { get; }   

        public GetAllClassesPageNumberQuery(int pageNumber, int pageSize, string? searchTerm = null)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            SearchTerm = searchTerm;
        }
    }
}
