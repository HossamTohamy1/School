using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Queries;
using School_managment.Infrastructure;
using School_managment.ViewModels;

namespace School_managment.Features.Classes.Handlers
{
    public class GetAllClassesHandler : IRequestHandler<GetAllClassesQuery, ResponseViewModel<List<ClassDto>>>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public GetAllClassesHandler(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ResponseViewModel<List<ClassDto>>> Handle(GetAllClassesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var classes = await _context.Classes
                    .Where(c => !c.IsDeleted)
                    .ProjectTo<ClassDto>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                return new SuccessResponseViewModel<List<ClassDto>>(classes, "Classes fetched successfully.");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<List<ClassDto>>($"Error fetching classes: {ex.Message}");
            }
        }
    }
}
