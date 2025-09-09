using School_managment.Features.Timetables.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Timetables.Orchestrators
{
    public interface ITimeTableOrchestrator
    {
        Task<ResponseViewModel<SmartTimetableGenerationResult>> GenerateSmartTimetableAsync(int classId, SmartTimetableRequest request, CancellationToken cancellationToken = default);
        Task<ResponseViewModel<List<ConflictDto>>> ValidateTimetableAsync(int timetableId, CancellationToken cancellationToken = default);
        Task<ResponseViewModel<TimetableStatisticsDto>> GetTimetableStatisticsAsync(int timetableId, CancellationToken cancellationToken = default);
        Task<ResponseViewModel<bool>> SwapTimetableSlotsAsync(int timetableId, SwapSlotsRequest request, CancellationToken cancellationToken = default);
        Task<ResponseViewModel<TeacherConflictResult>> CheckTeacherConflictAsync(int teacherId, DayOfWeek dayOfWeek, int period, int classId, CancellationToken cancellationToken = default);
        Task<ResponseViewModel<ConflictResolutionResult>> ResolveTeacherConflictsAsync(int timetableId, List<ConflictDto> conflicts, CancellationToken cancellationToken = default);
        Task<ResponseViewModel<List<AvailableTeacherDto>>> GetAvailableTeachersForSlotAsync(int classId, DayOfWeek dayOfWeek, int period, CancellationToken cancellationToken = default);
        Task<ResponseViewModel<SlotAssignmentResult>> AssignTeacherToSlotAsync(SlotAssignmentRequest request, CancellationToken cancellationToken = default);
        Task<ResponseViewModel<List<ConflictPreventionSuggestion>>> GetConflictPreventionSuggestionsAsync(int classId, CancellationToken cancellationToken = default);
    }
}
