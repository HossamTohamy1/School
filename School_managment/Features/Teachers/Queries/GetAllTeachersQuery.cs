using MediatR;
using School_managment.Features.Teachers.DTOs;
using School_managment.ViewModels;
using System.Collections.Generic;

namespace School_managment.Features.Teachers.Queries
{
    public class GetAllTeachersQuery : IRequest<ResponseViewModel<List<TeacherDto>>>
    {
    }
}
