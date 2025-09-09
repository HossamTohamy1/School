using MediatR;
using School_managment.Features.Timetables.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Timetables.Commands
{
    public class UpdateTimeTableCommand : IRequest<ResponseViewModel<TimeTableDto>>
    {
        public UpdateTimeTableDto TimeTable { get; set; }

        public UpdateTimeTableCommand(UpdateTimeTableDto timeTable)
        {
            TimeTable = timeTable;
        }
    }
    
}
