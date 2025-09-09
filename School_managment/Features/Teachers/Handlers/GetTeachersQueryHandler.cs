using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Enums;
using School_managment.Common.Models;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Queries;
using School_managment.Infrastructure;
using School_managment.ViewModels;

namespace School_managment.Features.Teachers.Handlers
{
    public class GetTeachersQueryHandler : IRequestHandler<GetTeachersQuery, ResponseViewModel<PagedResult<TeacherDto>>>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public GetTeachersQueryHandler(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ResponseViewModel<PagedResult<TeacherDto>>> Handle(GetTeachersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _context.Teachers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower().Trim();
                    query = query.Where(t =>
                        t.Name.ToLower().Contains(searchTerm) ||
                        t.Subject.ToLower().Contains(searchTerm)
                    );
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ProjectTo<TeacherDto>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                var result = new PagedResult<TeacherDto>
                {
                    Items = items,
                    TotalCount = totalCount
                };

                return new SuccessResponseViewModel<PagedResult<TeacherDto>>(result, "Teachers fetched successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<PagedResult<TeacherDto>>(
                    $"Error fetching teachers: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }
    }
}
