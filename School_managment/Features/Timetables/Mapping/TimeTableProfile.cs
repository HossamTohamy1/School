using AutoMapper;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Models;

namespace School_managment.Features.Timetables.Mapping
{
    public class TimeTableProfile : Profile
    {
        public TimeTableProfile()
        {
            CreateMap<TimeTable, TimeTableDto>()
                .ForMember(dest => dest.ClassName, opt => opt.MapFrom(src => $"{src.Class.Grade}-{src.Class.Section}"));

            CreateMap<TimetableSlot, TimetableSlotDto>()
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.Name : null))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher != null ? src.Teacher.Name : null));

            CreateMap<TeacherAvailability, TeacherAvailabilityDto>()
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher.Name));

            CreateMap<AddTimeTableDto, TimeTable>()
                .ForMember(dest => dest.GeneratedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

            CreateMap<AddTimetableSlotDto, TimetableSlot>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // ✅ العكس علشان GenerateSmartTimetableAsync
            CreateMap<TimetableSlot, AddTimetableSlotDto>()
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => src.Period))
            .ForMember(dest => dest.DayOfWeek, opt => opt.MapFrom(src => src.DayOfWeek))
            .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.SubjectId))
            .ForMember(dest => dest.TeacherId, opt => opt.MapFrom(src => src.TeacherId));

            CreateMap<UpdateTimeTableDto, TimeTable>();
            CreateMap<UpdateTimetableSlotDto, TimetableSlot>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}
