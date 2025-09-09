using MediatR;
using School_managment.Features.Timetables.DTOs;

namespace School_managment.Features.Timetables.Commands
{
    internal class CreateTimetableCommand : IRequest<int>
    {
        public int ClassId { get; set; }
        public string Name { get; set; }
        public List<AddTimetableSlotDto> Slots { get; set; }
    }
}