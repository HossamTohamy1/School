using School_managment.Features.Timetables.Models;
using School_managment.Features.Timetables.Orchestrators;

namespace School_managment.Features.Timetables.DTOs
{
    public class TimeTableDto
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string ScheduleId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool IsActive { get; set; }
        public string Constraints { get; set; }

        public List<TimetableSlotDto> TimetableSlots { get; set; } = new();
    }





    // 

    public class UpdateStatusRequest
    {
        public bool IsActive { get; set; }
    }


    public class AssignedTeacherDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public List<string> SubjectNames { get; set; } = new();
        public int SubjectCount { get; set; }
    }

    public class ClassAssignmentStatusDto
    {
        public int ClassId { get; set; }
        public int TotalSubjects { get; set; }
        public int AssignedSubjects { get; set; }
        public List<string> UnassignedSubjects { get; set; } = new();
        public bool IsFullyAssigned { get; set; }
        public double CompletionPercentage { get; set; }
    }

    public class AssignTeacherDto
    {
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int TeacherId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int Period { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
    }

    public class CheckTeacherConflictDto
    {
        public int TeacherId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
        public int ClassId { get; set; }
    }

    public class AdvancedTimetableStatistics
    {
        public TimetableStatisticsDto BasicStatistics { get; set; } = new();
        public ConflictAnalysis ConflictAnalysis { get; set; } = new();
        public UtilizationAnalysis UtilizationAnalysis { get; set; } = new();
    }

    public class ConflictAnalysis
    {
        public int TotalConflicts { get; set; }
        public int TeacherConflicts { get; set; }
        public int RestrictedPeriodConflicts { get; set; }
        public int InvalidAssignmentConflicts { get; set; }
        public Dictionary<string, int> ConflictsByDay { get; set; } = new();
        public Dictionary<string, int> ConflictsByPeriod { get; set; } = new();
    }

    public class UtilizationAnalysis
    {
        public int TotalPossibleSlots { get; set; }
        public int UsedSlots { get; set; }
        public double UtilizationPercentage { get; set; }
    }

    public class ClassGenerationReadiness
    {
        public int ClassId { get; set; }
        public bool IsReady { get; set; }
        public double ReadinessScore { get; set; }
        public List<ConflictPreventionSuggestion> CriticalIssues { get; set; } = new();
        public List<ConflictPreventionSuggestion> Warnings { get; set; } = new();
        public ClassAssignmentStatusDto AssignmentStatus { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class TimetableQualityReport
    {
        public int TimetableId { get; set; }
        public double OverallQualityScore { get; set; }
        public TimetableStatisticsDto Statistics { get; set; } = new();
        public ConflictSummary ConflictSummary { get; set; } = new();
        public QualityMetrics QualityMetrics { get; set; } = new();
        public List<string> ImprovementSuggestions { get; set; } = new();
    }

    public class ConflictSummary
    {
        public int TotalConflicts { get; set; }
        public int CriticalConflicts { get; set; }
        public int MinorConflicts { get; set; }
        public Dictionary<string, int> ConflictsByType { get; set; } = new();
    }

    public class QualityMetrics
    {
        public double ConflictRate { get; set; }
        public double UtilizationRate { get; set; }
        public double BalanceScore { get; set; }
        public string OverallRating { get; set; } = string.Empty;
    }

    public class TeacherAvailabilityMatrix
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public Dictionary<string, Dictionary<int, SlotAvailabilityInfo>> AvailabilityGrid { get; set; } = new();
    }

    public class SlotAvailabilityInfo
    {
        public bool IsAvailable { get; set; }
        public string? ConflictReason { get; set; }
        public string? ConflictingClassName { get; set; }
        public string? ConflictingSubject { get; set; }
    }

    public class BulkConflictCheckRequest
    {
        public List<SlotConflictCheckDto> SlotsToCheck { get; set; } = new();
    }

    public class SlotConflictCheckDto
    {
        public int TeacherId { get; set; }
        public int ClassId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
    }

    public class BulkConflictCheckResult
    {
        public List<SlotConflictResult> SlotResults { get; set; } = new();
        public int TotalChecked { get; set; }
        public int AvailableSlots { get; set; }
        public int ConflictingSlots { get; set; }
    }

    public class SlotConflictResult
    {
        public SlotConflictCheckDto SlotRequest { get; set; } = new();
        public TeacherConflictResult ConflictResult { get; set; } = new();
    }

    public class OptimizationSuggestion
    {
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class TimetableGenerationContext
    {
        public int CurrentClassId { get; set; }
        public List<School_managment.Common.Models.ClassTeacher> ClassTeacherAssignments { get; set; } = new();
        public List<TeacherAvailability> TeacherAvailabilities { get; set; } = new();
        public List<TimetableSlot> ExistingActiveSlots { get; set; } = new();
        public Dictionary<int, int> TeacherUsageTracker { get; set; } = new();
        public Dictionary<int, List<int>> SubjectTeacherMapping { get; set; } = new(); // SubjectId -> List of TeacherIds
    }

    public class SmartTimetableGenerationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeTableDto? TimetableDto { get; set; }
        public List<string> GenerationWarnings { get; set; } = new();
        public Dictionary<string, int> UnassignedSubjectHours { get; set; } = new();
        public int ConflictsGenerated { get; set; }
        public int TotalSlotsGenerated { get; set; }
        public List<string> OptimizationSuggestions { get; set; } = new();
    }

    public class AvailableTeacherDto
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public List<string> UnavailableReasons { get; set; } = new();
    }

    public class SlotAssignmentRequest
    {
        public int TimetableId { get; set; }
        public int ClassId { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
    }

    public class SlotAssignmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public TeacherConflictResult? ConflictDetails { get; set; }
    }

    public class ConflictPreventionSuggestion
    {
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public List<string> AffectedSubjects { get; set; } = new();
        public List<string> AffectedTeachers { get; set; } = new();
    }

    public class ConflictResolutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ResolvedCount { get; set; }
        public int UnresolvedCount { get; set; }
        public List<string> ResolutionDetails { get; set; } = new();
    }

    public class SmartTimetableRequest
    {
        public int MaxPeriodsPerDay { get; set; } = 8;
        public TimetableConstraints Constraints { get; set; } = new();
    }

    public class TimetableConstraints
    {
        public bool AvoidDoubleBooking { get; set; } = true;
        public bool SpreadSubjectsEvenly { get; set; } = true;
        public bool RespectRestrictedPeriods { get; set; } = true;
        public bool BalanceWorkload { get; set; } = false;
        public bool AllowConsecutiveClasses { get; set; } = false;
    }

    public class ConflictDto
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SlotId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
    }

    public class TimetableStatisticsDto
    {
        public int TimetableId { get; set; }
        public int TotalSlots { get; set; }
        public int FilledSlots { get; set; }
        public int EmptySlots { get; set; }
        public Dictionary<string, int> SubjectDistribution { get; set; } = new();
        public Dictionary<string, int> TeacherWorkload { get; set; } = new();
        public Dictionary<string, int> DailyDistribution { get; set; } = new();
    }

    public class SwapSlotsRequest
    {
        public SlotPosition Slot1 { get; set; } = new();
        public SlotPosition Slot2 { get; set; } = new();
    }

    public class SlotPosition
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
    }

    public class TeacherConflictResult
    {
        public bool IsAvailable { get; set; }
        public int? ConflictingClassId { get; set; }
        public string? ConflictingClassName { get; set; }
        public string? ConflictingSubjectName { get; set; }
        public string? ConflictMessage { get; set; }
    }


}
