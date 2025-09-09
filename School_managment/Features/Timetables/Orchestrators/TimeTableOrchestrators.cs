using AutoMapper;
using global::School_managment.Features.Timetables.Commands;
using global::School_managment.Features.Timetables.DTOs;
using global::School_managment.Features.Timetables.Models;
using global::School_managment.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Teachers.Models;
using School_managment.ViewModels;
using School_managment.Common.Enums;
using System.Linq;
using System.Text.Json;
using System.Xml;
using School_managment.Features.Timetables.Queries;

namespace School_managment.Features.Timetables.Orchestrators
{
  

    public class TimeTableOrchestrator : ITimeTableOrchestrator
    {
        private readonly IMediator _mediator;
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public TimeTableOrchestrator(IMediator mediator, SchoolDbContext context, IMapper mapper)
        {
            _mediator = mediator;
            _context = context;
            _mapper = mapper;
        }

        #region Smart Timetable Generation with Conflict Prevention

      
        public async Task<ResponseViewModel<SmartTimetableGenerationResult>> GenerateSmartTimetableAsync(
            int classId,
            SmartTimetableRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new SmartTimetableGenerationResult();

                var classTeacherAssignments = await GetClassTeacherAssignmentsAsync(classId, cancellationToken);
                if (!classTeacherAssignments.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = $"No teacher assignments found for class {classId}";
                    return new ErrorResponseViewModel<SmartTimetableGenerationResult>(
                        result.ErrorMessage,
                        ErrorCode.BadRequest);
                }

                var generationContext = await PrepareGenerationContextAsync(classId, classTeacherAssignments, cancellationToken);

                var (slots, warnings, unassignedHours) = await GenerateConflictFreeSlotsEnhanced(
                    generationContext,
                    request,
                    cancellationToken);

                var timetableId = await CreateTimetableAsync(classId, slots, cancellationToken);

                var timetableResponse = await _mediator.Send(new GetByIdTimeTableQuery(timetableId), cancellationToken);

                if (!timetableResponse.IsSuccess)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to retrieve created timetable";
                    return new ErrorResponseViewModel<SmartTimetableGenerationResult>(
                        result.ErrorMessage,
                        ErrorCode.InternalError);
                }

                result.Success = true;
                result.TimetableDto = timetableResponse.Data;
                result.GenerationWarnings = warnings;
                result.UnassignedSubjectHours = unassignedHours;
                result.ConflictsGenerated = 0;
                result.TotalSlotsGenerated = slots.Count;
                result.OptimizationSuggestions = GenerateOptimizationSuggestions(generationContext, slots);

                return new SuccessResponseViewModel<SmartTimetableGenerationResult>(
                    result,
                    "Smart timetable generated successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<SmartTimetableGenerationResult>(
                    $"Failed to generate smart timetable: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

  
        private async Task<(List<AddTimetableSlotDto> slots, List<string> warnings, Dictionary<string, int> unassignedHours)>
            GenerateConflictFreeSlotsEnhanced(
                TimetableGenerationContext context,
                SmartTimetableRequest request,
                CancellationToken cancellationToken)
        {
            var slots = new List<AddTimetableSlotDto>();
            var warnings = new List<string>();
            var unassignedHours = new Dictionary<string, int>();

            var subjectPool = CreateEnhancedSubjectPool(context.ClassTeacherAssignments);

            var daysOfWeek = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };

            var dailySubjectCount = new Dictionary<(DayOfWeek day, int subjectId), int>();

            foreach (var day in daysOfWeek)
            {
                for (int period = 1; period <= request.MaxPeriodsPerDay; period++)
                {
                    var availableAssignments = await GetAvailableAssignmentsForSlotEnhanced(
                        subjectPool,
                        context,
                        day,
                        period,
                        slots,
                        dailySubjectCount,
                        request.Constraints,
                        cancellationToken
                    );

                    if (availableAssignments.Any())
                    {
                        var selectedAssignment = SelectBestTeacherForSlot(
                            availableAssignments,
                            context,
                            day,
                            period,
                            dailySubjectCount,
                            request.Constraints
                        );

                        slots.Add(new AddTimetableSlotDto
                        {
                            Period = period,
                            DayOfWeek = day,
                            SubjectId = selectedAssignment.SubjectId,
                            TeacherId = selectedAssignment.TeacherId
                        });

                        RemoveSelectedAssignmentFromPool(subjectPool, selectedAssignment);
                        UpdateDailySubjectCount(dailySubjectCount, day, selectedAssignment.SubjectId);

                        context.TeacherUsageTracker[selectedAssignment.TeacherId] =
                            context.TeacherUsageTracker.GetValueOrDefault(selectedAssignment.TeacherId, 0) + 1;
                    }
                    else
                    {
                        warnings.Add($"No available teacher for {day} Period {period}");
                    }
                }
            }

            unassignedHours = CalculateUnassignedHours(subjectPool);
            return (slots, warnings, unassignedHours);
        }

        private void RemoveSelectedAssignmentFromPool(
            List<School_managment.Common.Models.ClassTeacher> pool,
            School_managment.Common.Models.ClassTeacher selectedAssignment)
        {
            var matchingAssignment = pool.FirstOrDefault(p =>
                p.TeacherId == selectedAssignment.TeacherId &&
                p.SubjectId == selectedAssignment.SubjectId &&
                p.ClassId == selectedAssignment.ClassId);

            if (matchingAssignment != null)
            {
                pool.Remove(matchingAssignment);
            }
        }

        private School_managment.Common.Models.ClassTeacher SelectBestTeacherForSlot(
            List<School_managment.Common.Models.ClassTeacher> availableAssignments,
            TimetableGenerationContext context,
            DayOfWeek day,
            int period,
            Dictionary<(DayOfWeek day, int subjectId), int> dailySubjectCount,
            TimetableConstraints constraints)
        {
            if (availableAssignments.Count == 1)
                return availableAssignments.First();

            var subjectGroups = availableAssignments.GroupBy(a => a.SubjectId).ToList();

            var selectedSubjectGroup = SelectBestSubject(subjectGroups, day, dailySubjectCount, constraints);
            var subjectCandidates = selectedSubjectGroup.ToList();

            if (subjectCandidates.Count == 1)
                return subjectCandidates.First();

            return SelectBestTeacherFromCandidates(subjectCandidates, context, day, period, constraints);
        }

        private School_managment.Common.Models.ClassTeacher SelectBestTeacherFromCandidates(
            List<School_managment.Common.Models.ClassTeacher> teacherCandidates,
            TimetableGenerationContext context,
            DayOfWeek day,
            int period,
            TimetableConstraints constraints)
        {
            if (teacherCandidates.Count == 1)
                return teacherCandidates.First();

            if (constraints.BalanceWorkload)
            {
                var minUsage = teacherCandidates.Min(t => context.TeacherUsageTracker.GetValueOrDefault(t.TeacherId, 0));
                var leastUsedTeachers = teacherCandidates
                    .Where(t => context.TeacherUsageTracker.GetValueOrDefault(t.TeacherId, 0) == minUsage)
                    .ToList();

                if (leastUsedTeachers.Count == 1)
                    return leastUsedTeachers.First();

                teacherCandidates = leastUsedTeachers;
            }

            var rotationSeed = (int)day * 10 + period;
            var selectedIndex = rotationSeed % teacherCandidates.Count;
            return teacherCandidates[selectedIndex];
        }

        private IGrouping<int, School_managment.Common.Models.ClassTeacher> SelectBestSubject(
            List<IGrouping<int, School_managment.Common.Models.ClassTeacher>> subjectGroups,
            DayOfWeek day,
            Dictionary<(DayOfWeek day, int subjectId), int> dailySubjectCount,
            TimetableConstraints constraints)
        {
            if (subjectGroups.Count == 1)
                return subjectGroups.First();

            if (constraints.SpreadSubjectsEvenly)
            {
                // Choose subject with least occurrences today
                var minDailyCount = subjectGroups.Min(g => dailySubjectCount.GetValueOrDefault((day, g.Key), 0));
                var candidateSubjects = subjectGroups
                    .Where(g => dailySubjectCount.GetValueOrDefault((day, g.Key), 0) == minDailyCount)
                    .ToList();

                if (candidateSubjects.Count == 1)
                    return candidateSubjects.First();

                subjectGroups = candidateSubjects;
            }

            // Random selection among remaining candidates
            var random = new Random();
            return subjectGroups[random.Next(subjectGroups.Count)];
        }


        private async Task<List<School_managment.Common.Models.ClassTeacher>> GetAvailableAssignmentsForSlotEnhanced(
            List<School_managment.Common.Models.ClassTeacher> subjectPool,
            TimetableGenerationContext context,
            DayOfWeek day,
            int period,
            List<AddTimetableSlotDto> currentSlots,
            Dictionary<(DayOfWeek day, int subjectId), int> dailySubjectCount,
            TimetableConstraints constraints,
            CancellationToken cancellationToken)
        {
            var availableAssignments = new List<School_managment.Common.Models.ClassTeacher>();

            var uniqueAssignments = subjectPool
                .GroupBy(p => new { p.TeacherId, p.SubjectId })
                .Select(g => g.First())
                .ToList();

            foreach (var assignment in uniqueAssignments)
            {
                if (assignment.TeacherId == 0 || assignment.Teacher == null)
                    continue;

                if (constraints.RespectRestrictedPeriods && !IsTeacherAvailable(assignment.Teacher, day, period))
                    continue;

                if (!IsTeacherAvailableFromDb(assignment.TeacherId, day, period, context.TeacherAvailabilities))
                    continue;

                if (constraints.AvoidDoubleBooking && HasTeacherConflictInCurrentTimetable(currentSlots, assignment.TeacherId, day, period))
                    continue;

                if (constraints.AvoidDoubleBooking && await HasTeacherConflictAcrossClassesAsync(assignment.TeacherId, day, period, context.CurrentClassId, cancellationToken))
                    continue;

                if (constraints.SpreadSubjectsEvenly && ShouldSkipForEvenDistribution(dailySubjectCount, day, assignment.SubjectId))
                    continue;

                if (!constraints.AllowConsecutiveClasses && HasConsecutiveConflict(currentSlots, assignment.SubjectId, day, period))
                    continue;

                var remainingHours = subjectPool.Count(p => p.TeacherId == assignment.TeacherId && p.SubjectId == assignment.SubjectId);
                if (remainingHours > 0)
                {
                    availableAssignments.Add(assignment);
                }
            }

            return availableAssignments;
        }

   
        private async Task<bool> HasTeacherConflictAcrossClassesAsync(
            int teacherId,
            DayOfWeek day,
            int period,
            int currentClassId,
            CancellationToken cancellationToken)
        {
            if (teacherId == 0) return false;

            var conflict = await _context.TimetableSlots
                .AnyAsync(ts =>
                    ts.TeacherId == teacherId &&
                    ts.DayOfWeek == day &&
                    ts.Period == period &&
                    ts.ClassId != currentClassId &&
                    ts.Timetable.IsActive,
                    cancellationToken);

            return conflict;
        }

        #endregion

        #region Slot Assignment and Availability Checking

    
        public async Task<ResponseViewModel<List<AvailableTeacherDto>>> GetAvailableTeachersForSlotAsync(
            int classId,
            DayOfWeek dayOfWeek,
            int period,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var classTeachers = await _context.ClassTeachers
                    .Where(ct => ct.ClassId == classId && ct.TeacherId != 0)
                    .ToListAsync(cancellationToken);

                var teacherIds = classTeachers.Select(ct => ct.TeacherId).ToList();
                var teachers = await _context.Teachers
                    .Where(t => teacherIds.Contains(t.Id))
                    .ToListAsync(cancellationToken);

                var subjectIds = classTeachers.Select(ct => ct.SubjectId).ToList();
                var subjects = await _context.Subjects
                    .Where(s => subjectIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                var availableTeachers = new List<AvailableTeacherDto>();

                foreach (var ct in classTeachers)
                {
                    var teacher = teachers.FirstOrDefault(t => t.Id == ct.TeacherId);
                    var subject = subjects.FirstOrDefault(s => s.Id == ct.SubjectId);

                    if (teacher == null) continue;

                    var availability = new AvailableTeacherDto
                    {
                        TeacherId = ct.TeacherId,
                        TeacherName = teacher.Name,
                        SubjectId = ct.SubjectId,
                        SubjectName = subject?.Name ?? "Unknown",
                        IsAvailable = true,
                        UnavailableReasons = new List<string>()
                    };

                    await CheckTeacherAvailabilityConstraints(availability, teacher, dayOfWeek, period, classId, cancellationToken);

                    availableTeachers.Add(availability);
                }

                return new SuccessResponseViewModel<List<AvailableTeacherDto>>(
                    availableTeachers.OrderByDescending(t => t.IsAvailable).ThenBy(t => t.TeacherName).ToList(),
                    $"Retrieved {availableTeachers.Count} available teachers");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<List<AvailableTeacherDto>>(
                    $"Error getting available teachers: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

        private async Task CheckTeacherAvailabilityConstraints(
            AvailableTeacherDto availability,
            Teacher teacher,
            DayOfWeek dayOfWeek,
            int period,
            int classId,
            CancellationToken cancellationToken)
        {
            if (!IsTeacherAvailable(teacher, dayOfWeek, period))
            {
                availability.IsAvailable = false;
                availability.UnavailableReasons.Add("Teacher has restricted period");
            }

            var dbAvailability = await _context.TeacherAvailabilities
                .FirstOrDefaultAsync(ta =>
                    ta.TeacherId == availability.TeacherId &&
                    ta.DayOfWeek == dayOfWeek &&
                    ta.Period == period,
                    cancellationToken);

            if (dbAvailability?.IsAvailable == false)
            {
                availability.IsAvailable = false;
                availability.UnavailableReasons.Add("Teacher marked as unavailable");
            }

            var hasConflict = await _context.TimetableSlots
                .AnyAsync(ts =>
                    ts.TeacherId == availability.TeacherId &&
                    ts.DayOfWeek == dayOfWeek &&
                    ts.Period == period &&
                    ts.ClassId != classId &&
                    ts.Timetable.IsActive,
                    cancellationToken);

            if (hasConflict)
            {
                availability.IsAvailable = false;
                availability.UnavailableReasons.Add("Teacher is assigned to another class at this time");
            }
        }

        public async Task<ResponseViewModel<SlotAssignmentResult>> AssignTeacherToSlotAsync(
            SlotAssignmentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new SlotAssignmentResult();

                var isValidAssignment = await _context.ClassTeachers
                    .AnyAsync(ct =>
                        ct.ClassId == request.ClassId &&
                        ct.TeacherId == request.TeacherId &&
                        ct.SubjectId == request.SubjectId,
                        cancellationToken);

                if (!isValidAssignment)
                {
                    result.Success = false;
                    result.ErrorMessage = "Teacher is not assigned to teach this subject in this class";
                    return new ErrorResponseViewModel<SlotAssignmentResult>(
                        result.ErrorMessage,
                        ErrorCode.BadRequest);
                }

                var conflictCheckResponse = await CheckTeacherConflictAsync(
                    request.TeacherId,
                    request.DayOfWeek,
                    request.Period,
                    request.ClassId,
                    cancellationToken);

                if (!conflictCheckResponse.IsSuccess || !conflictCheckResponse.Data.IsAvailable)
                {
                    result.Success = false;
                    result.ErrorMessage = conflictCheckResponse.Data?.ConflictMessage ?? "Teacher conflict check failed";
                    result.ConflictDetails = conflictCheckResponse.Data;
                    return new ErrorResponseViewModel<SlotAssignmentResult>(
                        result.ErrorMessage,
                        ErrorCode.BadRequest);
                }

                var existingSlot = await _context.TimetableSlots
                    .FirstOrDefaultAsync(ts =>
                        ts.TimetableId == request.TimetableId &&
                        ts.DayOfWeek == request.DayOfWeek &&
                        ts.Period == request.Period,
                        cancellationToken);

                if (existingSlot != null)
                {
                    existingSlot.TeacherId = request.TeacherId;
                    existingSlot.SubjectId = request.SubjectId;
                }
                else
                {
                    var newSlot = new TimetableSlot
                    {
                        TimetableId = request.TimetableId,
                        ClassId = request.ClassId,
                        Period = request.Period,
                        DayOfWeek = request.DayOfWeek,
                        SubjectId = request.SubjectId,
                        TeacherId = request.TeacherId
                    };
                    _context.TimetableSlots.Add(newSlot);
                }

                await _context.SaveChangesAsync(cancellationToken);

                result.Success = true;
                result.Message = "Teacher assigned successfully";

                return new SuccessResponseViewModel<SlotAssignmentResult>(
                    result,
                    "Teacher assigned to slot successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<SlotAssignmentResult>(
                    $"Error assigning teacher: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

        #endregion

        #region Conflict Prevention Suggestions

        public async Task<ResponseViewModel<List<ConflictPreventionSuggestion>>> GetConflictPreventionSuggestionsAsync(
            int classId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var suggestions = new List<ConflictPreventionSuggestion>();

                var classTeachers = await _context.ClassTeachers
                    .Where(ct => ct.ClassId == classId)
                    .ToListAsync(cancellationToken);

                var teacherIds = classTeachers.Where(ct => ct.TeacherId != 0).Select(ct => ct.TeacherId).ToList();
                var teachers = await _context.Teachers
                    .Where(t => teacherIds.Contains(t.Id))
                    .ToListAsync(cancellationToken);

                var subjectIds = classTeachers.Select(ct => ct.SubjectId).ToList();
                var subjects = await _context.Subjects
                    .Where(s => subjectIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                var unassignedSubjects = classTeachers.Where(ct => ct.TeacherId == 0).ToList();
                if (unassignedSubjects.Any())
                {
                    var unassignedSubjectNames = unassignedSubjects
                        .Select(us => subjects.FirstOrDefault(s => s.Id == us.SubjectId)?.Name ?? "Unknown")
                        .ToList();

                    suggestions.Add(new ConflictPreventionSuggestion
                    {
                        Type = "UnassignedSubjects",
                        Priority = "High",
                        Description = $"You have {unassignedSubjects.Count} subjects without assigned teachers",
                        AffectedSubjects = unassignedSubjectNames,
                        Recommendation = "Assign teachers to these subjects before generating timetable"
                    });
                }

                var teachersWithRestrictions = teachers
                    .Where(t => t.RestrictedPeriods?.Any() == true)
                    .ToList();

                if (teachersWithRestrictions.Any())
                {
                    suggestions.Add(new ConflictPreventionSuggestion
                    {
                        Type = "RestrictedPeriods",
                        Priority = "Medium",
                        Description = $"{teachersWithRestrictions.Count} teachers have restricted periods",
                        AffectedTeachers = teachersWithRestrictions.Select(t => t.Name).ToList(),
                        Recommendation = "Review teacher availabilities to maximize scheduling flexibility"
                    });
                }

                var teacherSubjectCounts = classTeachers
                    .Where(ct => ct.TeacherId != 0)
                    .GroupBy(ct => ct.TeacherId)
                    .Select(g => new
                    {
                        TeacherId = g.Key,
                        TotalHours = g.Sum(ct => subjects.FirstOrDefault(s => s.Id == ct.SubjectId)?.HoursPerWeek ?? 0)
                    })
                    .Where(x => x.TotalHours > 20)
                    .ToList();

                if (teacherSubjectCounts.Any())
                {
                    suggestions.Add(new ConflictPreventionSuggestion
                    {
                        Type = "TeacherOverload",
                        Priority = "Medium",
                        Description = $"{teacherSubjectCounts.Count} teachers may be overloaded",
                        Recommendation = "Consider redistributing subjects to balance teacher workload"
                    });
                }

                return new SuccessResponseViewModel<List<ConflictPreventionSuggestion>>(
                    suggestions,
                    $"Generated {suggestions.Count} conflict prevention suggestions");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<List<ConflictPreventionSuggestion>>(
                    $"Error generating suggestions: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

        #endregion

        #region Validation and Conflict Checking

        public async Task<ResponseViewModel<TeacherConflictResult>> CheckTeacherConflictAsync(
            int teacherId,
            DayOfWeek dayOfWeek,
            int period,
            int classId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var teacher = await _context.Teachers.FindAsync(teacherId);
                if (teacher == null)
                {
                    var errorResult = new TeacherConflictResult
                    {
                        IsAvailable = false,
                        ConflictMessage = "Teacher not found"
                    };

                    return new ErrorResponseViewModel<TeacherConflictResult>(
                        errorResult.ConflictMessage,
                        ErrorCode.NotFound);
                }

                if (!IsTeacherAvailable(teacher, dayOfWeek, period))
                {
                    var restrictedResult = new TeacherConflictResult
                    {
                        IsAvailable = false,
                        ConflictMessage = $"Teacher has restricted period on {dayOfWeek} Period {period}"
                    };

                    return new SuccessResponseViewModel<TeacherConflictResult>(
                        restrictedResult,
                        "Teacher conflict check completed");
                }

                var availability = await _context.TeacherAvailabilities
                    .FirstOrDefaultAsync(ta =>
                        ta.TeacherId == teacherId &&
                        ta.DayOfWeek == dayOfWeek &&
                        ta.Period == period,
                        cancellationToken);

                if (availability?.IsAvailable == false)
                {
                    var unavailableResult = new TeacherConflictResult
                    {
                        IsAvailable = false,
                        ConflictMessage = $"Teacher is marked as unavailable on {dayOfWeek} Period {period}"
                    };

                    return new SuccessResponseViewModel<TeacherConflictResult>(
                        unavailableResult,
                        "Teacher conflict check completed");
                }

                var conflictingSlots = await _context.TimetableSlots
                    .Where(ts => ts.TeacherId == teacherId &&
                                 ts.DayOfWeek == dayOfWeek &&
                                 ts.Period == period &&
                                 ts.ClassId != classId &&
                                 ts.Timetable.IsActive)
                    .ToListAsync(cancellationToken);

                if (conflictingSlots.Any())
                {
                    var conflictingSlot = conflictingSlots.First();

                    var conflictingClass = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Id == conflictingSlot.ClassId, cancellationToken);
                    var conflictingSubject = await _context.Subjects
                        .FirstOrDefaultAsync(s => s.Id == conflictingSlot.SubjectId, cancellationToken);

                    var conflictResult = new TeacherConflictResult
                    {
                        IsAvailable = false,
                        ConflictingClassId = conflictingSlot.ClassId,
                        ConflictingClassName = conflictingClass != null ? $"{conflictingClass.Grade}/{conflictingClass.Section}" : "Unknown Class",
                        ConflictingSubjectName = conflictingSubject?.Name ?? "Unknown Subject",
                        ConflictMessage = $"Teacher is already assigned to {conflictingClass?.Grade}/{conflictingClass?.Section} " +
                                         $"for {conflictingSubject?.Name} on {dayOfWeek} Period {period}"
                    };

                    return new SuccessResponseViewModel<TeacherConflictResult>(
                        conflictResult,
                        "Teacher conflict check completed");
                }

                var availableResult = new TeacherConflictResult { IsAvailable = true };
                return new SuccessResponseViewModel<TeacherConflictResult>(
                    availableResult,
                    "Teacher is available for this slot");
            }
            catch (Exception ex)
            {
                var errorResult = new TeacherConflictResult
                {
                    IsAvailable = false,
                    ConflictMessage = $"Error checking teacher availability: {ex.Message}"
                };

                return new ErrorResponseViewModel<TeacherConflictResult>(
                    $"Error checking teacher availability: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

        public async Task<ResponseViewModel<List<ConflictDto>>> ValidateTimetableAsync(
            int timetableId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var timetable = await _context.Timetables
                    .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);

                if (timetable == null)
                {
                    return new ErrorResponseViewModel<List<ConflictDto>>(
                        $"Timetable with ID {timetableId} not found.",
                        ErrorCode.NotFound);
                }

                var timetableSlots = await _context.TimetableSlots
                    .Where(ts => ts.TimetableId == timetableId)
                    .ToListAsync(cancellationToken);

                var teacherIds = timetableSlots.Where(ts => ts.TeacherId.HasValue && ts.TeacherId != 0)
                    .Select(ts => ts.TeacherId.Value).Distinct().ToList();
                var teachers = await _context.Teachers
                    .Where(t => teacherIds.Contains(t.Id))
                    .ToListAsync(cancellationToken);

                var subjectIds = timetableSlots.Where(ts => ts.SubjectId.HasValue)
                    .Select(ts => ts.SubjectId.Value).Distinct().ToList();
                var subjects = await _context.Subjects
                    .Where(s => subjectIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                var classIds = timetableSlots.Select(ts => ts.ClassId).Distinct().ToList();
                var classes = await _context.Classes
                    .Where(c => classIds.Contains(c.Id))
                    .ToListAsync(cancellationToken);

                var conflicts = new List<ConflictDto>();

                var internalTeacherConflicts = timetableSlots
                    .Where(ts => ts.TeacherId != null && ts.TeacherId != 0)
                    .GroupBy(ts => new { ts.TeacherId, ts.DayOfWeek, ts.Period })
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.Select(ts =>
                    {
                        var teacher = teachers.FirstOrDefault(t => t.Id == ts.TeacherId);
                        return new ConflictDto
                        {
                            Type = "TeacherDoubleBooking",
                            Description = $"Teacher {teacher?.Name ?? "Unknown"} has multiple classes at {ts.DayOfWeek} Period {ts.Period} in same timetable",
                            SlotId = ts.Id,
                            DayOfWeek = ts.DayOfWeek,
                            Period = ts.Period
                        };
                    }));

                conflicts.AddRange(internalTeacherConflicts);

                foreach (var slot in timetableSlots.Where(ts => ts.TeacherId != null && ts.TeacherId != 0))
                {
                    var conflictingSlots = await _context.TimetableSlots
                        .Where(ts => ts.TeacherId == slot.TeacherId &&
                                    ts.DayOfWeek == slot.DayOfWeek &&
                                    ts.Period == slot.Period &&
                                    ts.ClassId != slot.ClassId &&
                                    ts.Id != slot.Id &&
                                    ts.Timetable.IsActive)
                        .ToListAsync(cancellationToken);

                    foreach (var conflictSlot in conflictingSlots)
                    {
                        var teacher = teachers.FirstOrDefault(t => t.Id == slot.TeacherId);
                        var slotClass = classes.FirstOrDefault(c => c.Id == slot.ClassId);
                        var conflictClass = classes.FirstOrDefault(c => c.Id == conflictSlot.ClassId);

                        conflicts.Add(new ConflictDto
                        {
                            Type = "TeacherCrossClassDoubleBooking",
                            Description = $"Teacher {teacher?.Name ?? "Unknown"} is assigned to both {slotClass?.Grade}/{slotClass?.Section} and {conflictClass?.Grade}/{conflictClass?.Section} at {slot.DayOfWeek} Period {slot.Period}",
                            SlotId = slot.Id,
                            DayOfWeek = slot.DayOfWeek,
                            Period = slot.Period
                        });
                    }
                }

                var restrictedConflicts = timetableSlots
                    .Where(ts => ts.TeacherId.HasValue && ts.TeacherId != 0)
                    .Where(ts =>
                    {
                        var teacher = teachers.FirstOrDefault(t => t.Id == ts.TeacherId);
                        return teacher != null && !IsTeacherAvailable(teacher, ts.DayOfWeek, ts.Period);
                    })
                    .Select(ts =>
                    {
                        var teacher = teachers.FirstOrDefault(t => t.Id == ts.TeacherId);
                        return new ConflictDto
                        {
                            Type = "RestrictedPeriod",
                            Description = $"Teacher {teacher?.Name ?? "Unknown"} cannot teach at {ts.DayOfWeek} Period {ts.Period} (restricted period).",
                            SlotId = ts.Id,
                            DayOfWeek = ts.DayOfWeek,
                            Period = ts.Period
                        };
                    });

                conflicts.AddRange(restrictedConflicts);

                var classId = timetable.ClassId;
                var assignedCombinations = await _context.ClassTeachers
                    .Where(cs => cs.ClassId == classId)
                    .Select(cs => new { cs.SubjectId, cs.TeacherId })
                    .ToListAsync(cancellationToken);

                var invalidAssignments = timetableSlots
                    .Where(ts => ts.SubjectId != null && ts.TeacherId != null && ts.TeacherId != 0)
                    .Where(ts => !assignedCombinations.Any(ac =>
                        ac.SubjectId == ts.SubjectId && ac.TeacherId == ts.TeacherId))
                    .Select(ts =>
                    {
                        var teacher = teachers.FirstOrDefault(t => t.Id == ts.TeacherId);
                        var subject = subjects.FirstOrDefault(s => s.Id == ts.SubjectId);

                        return new ConflictDto
                        {
                            Type = "InvalidTeacherSubjectAssignment",
                            Description = $"Teacher {teacher?.Name ?? "Unknown"} is not assigned to teach {subject?.Name ?? "Unknown Subject"} in this class",
                            SlotId = ts.Id,
                            DayOfWeek = ts.DayOfWeek,
                            Period = ts.Period
                        };
                    });

                conflicts.AddRange(invalidAssignments);

                return new SuccessResponseViewModel<List<ConflictDto>>(
                    conflicts,
                    $"Validation completed. Found {conflicts.Count} conflicts.");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<List<ConflictDto>>(
                    $"Error validating timetable: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

        public async Task<ResponseViewModel<TimetableStatisticsDto>> GetTimetableStatisticsAsync(
            int timetableId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var timetable = await _context.Timetables
                    .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);

                if (timetable == null)
                {
                    return new ErrorResponseViewModel<TimetableStatisticsDto>(
                        $"Timetable with ID {timetableId} not found.",
                        ErrorCode.NotFound);
                }

                var timetableSlots = await _context.TimetableSlots
                    .Where(ts => ts.TimetableId == timetableId)
                    .ToListAsync(cancellationToken);

                var subjectIds = timetableSlots.Where(ts => ts.SubjectId.HasValue)
                    .Select(ts => ts.SubjectId.Value).Distinct().ToList();
                var subjects = await _context.Subjects
                    .Where(s => subjectIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                var teacherIds = timetableSlots.Where(ts => ts.TeacherId.HasValue && ts.TeacherId != 0)
                    .Select(ts => ts.TeacherId.Value).Distinct().ToList();
                var teachers = await _context.Teachers
                    .Where(t => teacherIds.Contains(t.Id))
                    .ToListAsync(cancellationToken);

                var stats = new TimetableStatisticsDto
                {
                    TimetableId = timetableId,
                    TotalSlots = timetableSlots.Count,
                    FilledSlots = timetableSlots.Count(ts => ts.SubjectId != null),
                    EmptySlots = timetableSlots.Count(ts => ts.SubjectId == null),
                    SubjectDistribution = timetableSlots
                        .Where(ts => ts.SubjectId.HasValue)
                        .GroupBy(ts => ts.SubjectId.Value)
                        .ToDictionary(
                            g => subjects.FirstOrDefault(s => s.Id == g.Key)?.Name ?? "Unknown Subject",
                            g => g.Count()),
                    TeacherWorkload = timetableSlots
                        .Where(ts => ts.TeacherId.HasValue && ts.TeacherId != 0)
                        .GroupBy(ts => ts.TeacherId.Value)
                        .ToDictionary(
                            g => teachers.FirstOrDefault(t => t.Id == g.Key)?.Name ?? "Unknown Teacher",
                            g => g.Count()),
                    DailyDistribution = timetableSlots
                        .Where(ts => ts.SubjectId != null)
                        .GroupBy(ts => ts.DayOfWeek)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count())
                };

                return new SuccessResponseViewModel<TimetableStatisticsDto>(
                    stats,
                    "Timetable statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<TimetableStatisticsDto>(
                    $"Error getting timetable statistics: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

        public async Task<ResponseViewModel<bool>> SwapTimetableSlotsAsync(
            int timetableId,
            SwapSlotsRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var timetable = await _context.Timetables
                    .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);

                if (timetable == null)
                {
                    return new ErrorResponseViewModel<bool>(
                        $"Timetable with ID {timetableId} not found.",
                        ErrorCode.NotFound);
                }

                var timetableSlots = await _context.TimetableSlots
                    .Where(ts => ts.TimetableId == timetableId)
                    .ToListAsync(cancellationToken);

                var slot1 = timetableSlots.FirstOrDefault(ts =>
                    ts.DayOfWeek == request.Slot1.DayOfWeek && ts.Period == request.Slot1.Period);
                var slot2 = timetableSlots.FirstOrDefault(ts =>
                    ts.DayOfWeek == request.Slot2.DayOfWeek && ts.Period == request.Slot2.Period);

                if (slot1 == null || slot2 == null)
                {
                    return new ErrorResponseViewModel<bool>(
                        "One or both slots not found",
                        ErrorCode.NotFound);
                }

                (slot1.SubjectId, slot2.SubjectId) = (slot2.SubjectId, slot1.SubjectId);
                (slot1.TeacherId, slot2.TeacherId) = (slot2.TeacherId, slot1.TeacherId);

                await _context.SaveChangesAsync(cancellationToken);

                return new SuccessResponseViewModel<bool>(
                    true,
                    "Slots swapped successfully");
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<bool>(
                    $"Error swapping slots: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

        public async Task<ResponseViewModel<ConflictResolutionResult>> ResolveTeacherConflictsAsync(
            int timetableId,
            List<ConflictDto> conflicts,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var timetable = await _context.Timetables
                    .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);

                if (timetable == null)
                {
                    return new ErrorResponseViewModel<ConflictResolutionResult>(
                        $"Timetable with ID {timetableId} not found",
                        ErrorCode.NotFound);
                }

                var timetableSlots = await _context.TimetableSlots
                    .Where(ts => ts.TimetableId == timetableId)
                    .ToListAsync(cancellationToken);

                var teacherIds = timetableSlots.Where(ts => ts.TeacherId.HasValue)
                    .Select(ts => ts.TeacherId.Value).Distinct().ToList();
                var teachers = await _context.Teachers
                    .Where(t => teacherIds.Contains(t.Id))
                    .ToListAsync(cancellationToken);

                var subjectIds = timetableSlots.Where(ts => ts.SubjectId.HasValue)
                    .Select(ts => ts.SubjectId.Value).Distinct().ToList();
                var subjects = await _context.Subjects
                    .Where(s => subjectIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                int resolvedCount = 0;
                int unresolvedCount = 0;
                var resolutionMessages = new List<string>();

                var doubleBookingConflicts = conflicts.Where(c =>
                    c.Type == "TeacherDoubleBooking" ||
                    c.Type == "TeacherCrossClassDoubleBooking").ToList();

                foreach (var conflict in doubleBookingConflicts)
                {
                    try
                    {
                        var conflictingSlot = timetableSlots
                            .FirstOrDefault(ts => ts.Id == conflict.SlotId);

                        if (conflictingSlot == null)
                        {
                            unresolvedCount++;
                            continue;
                        }

                        var alternativeSlot = await FindAlternativeSlotAsync(
                            conflictingSlot.TeacherId ?? 0,
                            conflictingSlot.SubjectId,
                            conflictingSlot.ClassId,
                            conflictingSlot.DayOfWeek,
                            conflictingSlot.Period,
                            timetableSlots.ToList(),
                            cancellationToken
                        );

                        if (alternativeSlot != null)
                        {
                            conflictingSlot.DayOfWeek = alternativeSlot.Value.DayOfWeek;
                            conflictingSlot.Period = alternativeSlot.Value.Period;

                            resolvedCount++;
                            var subject = subjects.FirstOrDefault(s => s.Id == conflictingSlot.SubjectId);
                            resolutionMessages.Add(
                                $"Moved {subject?.Name ?? "Unknown Subject"} to {alternativeSlot.Value.DayOfWeek} Period {alternativeSlot.Value.Period}"
                            );
                        }
                        else
                        {
                            _context.TimetableSlots.Remove(conflictingSlot);
                            resolvedCount++;
                            var subject = subjects.FirstOrDefault(s => s.Id == conflictingSlot.SubjectId);
                            resolutionMessages.Add(
                                $"Removed conflicting slot: {subject?.Name ?? "Unknown Subject"} on {conflictingSlot.DayOfWeek} Period {conflictingSlot.Period}"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        unresolvedCount++;
                        resolutionMessages.Add($"Error resolving conflict: {ex.Message}");
                    }
                }

                if (resolvedCount > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                var result = new ConflictResolutionResult
                {
                    Success = resolvedCount > 0,
                    Message = resolvedCount > 0
                        ? $"Resolved {resolvedCount} conflict(s), {unresolvedCount} unresolved"
                        : $"Could not resolve any conflicts",
                    ResolvedCount = resolvedCount,
                    UnresolvedCount = unresolvedCount,
                    ResolutionDetails = resolutionMessages
                };

                return new SuccessResponseViewModel<ConflictResolutionResult>(
                    result,
                    result.Message);
            }
            catch (Exception ex)
            {
                return new ErrorResponseViewModel<ConflictResolutionResult>(
                    $"Error resolving conflicts: {ex.Message}",
                    ErrorCode.InternalError);
            }
        }

        #endregion

        #region Helper Methods

    
        private async Task<TimetableGenerationContext> PrepareGenerationContextAsync(
          int classId,
          List<School_managment.Common.Models.ClassTeacher> classTeacherAssignments,
          CancellationToken cancellationToken)
        {
            var teacherIds = classTeacherAssignments
                .Where(ct => ct.TeacherId != 0)
                .Select(ct => ct.TeacherId)
                .Distinct()
                .ToList();

            var teacherAvailabilities = await _context.TeacherAvailabilities
                .Where(ta => teacherIds.Contains(ta.TeacherId))
                .ToListAsync(cancellationToken);

            var existingSlots = await _context.TimetableSlots
                .Where(ts => ts.Timetable.IsActive &&
                             ts.TeacherId != null &&
                             ts.TeacherId != 0 &&
                             teacherIds.Contains(ts.TeacherId.Value))
                .ToListAsync(cancellationToken);

            var subjectTeacherMapping = classTeacherAssignments
                .Where(ct => ct.TeacherId != 0)
                .GroupBy(ct => ct.SubjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ct => ct.TeacherId).Distinct().ToList()
                );

            return new TimetableGenerationContext
            {
                CurrentClassId = classId,
                ClassTeacherAssignments = classTeacherAssignments,
                TeacherAvailabilities = teacherAvailabilities,
                ExistingActiveSlots = existingSlots,
                TeacherUsageTracker = new Dictionary<int, int>(),
                SubjectTeacherMapping = subjectTeacherMapping
            };
        }

        private async Task<List<School_managment.Common.Models.ClassTeacher>> GetClassTeacherAssignmentsAsync(
            int classId,
            CancellationToken cancellationToken)
        {
            var classTeachers = await _context.ClassTeachers
                .Where(ct => ct.ClassId == classId)
                .ToListAsync(cancellationToken);

            var teacherIds = classTeachers.Where(ct => ct.TeacherId != 0).Select(ct => ct.TeacherId).ToList();
            var teachers = await _context.Teachers
                .Where(t => teacherIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            var subjectIds = classTeachers.Select(ct => ct.SubjectId).ToList();
            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            foreach (var ct in classTeachers)
            {
                ct.Teacher = teachers.FirstOrDefault(t => t.Id == ct.TeacherId);
                ct.Subject = subjects.FirstOrDefault(s => s.Id == ct.SubjectId);
            }

            return classTeachers;
        }

        private List<School_managment.Common.Models.ClassTeacher> CreateEnhancedSubjectPool(
            List<School_managment.Common.Models.ClassTeacher> assignments)
        {
            var pool = new List<School_managment.Common.Models.ClassTeacher>();

            var subjectGroups = assignments
                .Where(a => a.TeacherId != 0 && a.Teacher != null && a.Subject != null)
                .GroupBy(a => a.SubjectId)
                .ToList();

            foreach (var subjectGroup in subjectGroups)
            {
                var subjectAssignments = subjectGroup.ToList();
                var totalHoursPerWeek = subjectAssignments.First().Subject.HoursPerWeek;

                if (subjectAssignments.Count == 1)
                {
                    var assignment = subjectAssignments.First();
                    for (int i = 0; i < totalHoursPerWeek; i++)
                    {
                        pool.Add(assignment);
                    }
                }
                else
                {
                    var distributedHours = DistributeHoursAmongTeachers(subjectAssignments, totalHoursPerWeek);

                    foreach (var teacherHours in distributedHours)
                    {
                        for (int i = 0; i < teacherHours.Hours; i++)
                        {
                            pool.Add(teacherHours.Assignment);
                        }
                    }
                }
            }

            return pool;
        }

      
        private List<(School_managment.Common.Models.ClassTeacher Assignment, int Hours)> DistributeHoursAmongTeachers(
            List<School_managment.Common.Models.ClassTeacher> subjectAssignments,
            int totalHours)
        {
            var result = new List<(School_managment.Common.Models.ClassTeacher Assignment, int Hours)>();
            var teacherCount = subjectAssignments.Count;
            var baseHours = totalHours / teacherCount;
            var remainingHours = totalHours % teacherCount;

            for (int i = 0; i < subjectAssignments.Count; i++)
            {
                var hours = baseHours + (i < remainingHours ? 1 : 0);
                result.Add((subjectAssignments[i], hours));
            }

            return result;
        }

        private void UpdateDailySubjectCount(
            Dictionary<(DayOfWeek day, int subjectId), int> dailyCount,
            DayOfWeek day,
            int subjectId)
        {
            var key = (day, subjectId);
            dailyCount[key] = dailyCount.GetValueOrDefault(key, 0) + 1;
        }

        private bool ShouldSkipForEvenDistribution(
            Dictionary<(DayOfWeek day, int subjectId), int> dailyCount,
            DayOfWeek day,
            int subjectId)
        {
            return dailyCount.GetValueOrDefault((day, subjectId), 0) >= 2;
        }

        private bool HasConsecutiveConflict(
            List<AddTimetableSlotDto> slots,
            int subjectId,
            DayOfWeek day,
            int period)
        {
            return slots.Any(s =>
                s.SubjectId == subjectId &&
                s.DayOfWeek == day &&
                Math.Abs(s.Period - period) == 1);
        }

        private Dictionary<string, int> CalculateUnassignedHours(
            List<School_managment.Common.Models.ClassTeacher> remainingPool)
        {
            return remainingPool
                .Where(r => r.Subject != null)
                .GroupBy(r => r.Subject.Name)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private List<string> GenerateOptimizationSuggestions(
            TimetableGenerationContext context,
            List<AddTimetableSlotDto> slots)
        {
            var suggestions = new List<string>();

            var dailyDistribution = slots.GroupBy(s => s.DayOfWeek).ToDictionary(g => g.Key, g => g.Count());
            if (dailyDistribution.Values.Any())
            {
                var maxDaily = dailyDistribution.Values.Max();
                var minDaily = dailyDistribution.Values.Min();

                if (maxDaily - minDaily > 2)
                {
                    suggestions.Add("Consider redistributing subjects for more balanced daily schedules");
                }
            }

            var teacherWorkload = slots.GroupBy(s => s.TeacherId).ToDictionary(g => g.Key, g => g.Count());
            if (teacherWorkload.Values.Any() && teacherWorkload.Values.Max() - teacherWorkload.Values.Min() > 3)
            {
                suggestions.Add("Some teachers have significantly more periods than others");
            }

            return suggestions;
        }

        private async Task<int> CreateTimetableAsync(
            int classId,
            List<AddTimetableSlotDto> slots,
            CancellationToken cancellationToken)
        {
            var createCommand = new CreateTimetableCommand
            {
                ClassId = classId,
                Name = $"Smart Timetable for Class {classId} - {DateTime.Now:yyyy-MM-dd}",
                Slots = slots
            };

            return await _mediator.Send(createCommand, cancellationToken);
        }

        private bool IsTeacherAvailable(Teacher teacher, DayOfWeek day, int period)
        {
            if (teacher?.RestrictedPeriods == null || !teacher.RestrictedPeriods.Any())
                return true;

            string currentDayKey = day switch
            {
                DayOfWeek.Sunday => "Sun",
                DayOfWeek.Monday => "Mon",
                DayOfWeek.Tuesday => "Tue",
                DayOfWeek.Wednesday => "Wed",
                DayOfWeek.Thursday => "Thu",
                DayOfWeek.Friday => "Fri",
                DayOfWeek.Saturday => "Sat",
                _ => day.ToString().Substring(0, 3)
            };

            foreach (var restrictedPeriod in teacher.RestrictedPeriods)
            {
                var parts = restrictedPeriod.Trim().Split('-');
                if (parts.Length != 2) continue;

                var dayKey = parts[0].Trim();
                if (!int.TryParse(parts[1].Trim(), out int restrictedPeriodNumber)) continue;

                if (string.Equals(dayKey, currentDayKey, StringComparison.OrdinalIgnoreCase) &&
                    restrictedPeriodNumber == period)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsTeacherAvailableFromDb(
            int teacherId,
            DayOfWeek day,
            int period,
            List<TeacherAvailability> availabilities)
        {
            if (teacherId == 0) return false;

            var availability = availabilities.FirstOrDefault(a =>
                a.TeacherId == teacherId &&
                a.DayOfWeek == day &&
                a.Period == period);

            return availability?.IsAvailable ?? true;
        }
        private bool HasTeacherConflictInCurrentTimetable(
            List<AddTimetableSlotDto> existingSlots,
            int teacherId,
            DayOfWeek day,
            int period)
        {
            if (teacherId == 0) return false;

            return existingSlots.Any(s =>
                s.TeacherId == teacherId &&
                s.DayOfWeek == day &&
                s.Period == period);
        }



        private async Task<(DayOfWeek DayOfWeek, int Period)?> FindAlternativeSlotAsync(
            int teacherId,
            int? subjectId,
            int classId,
            DayOfWeek originalDay,
            int originalPeriod,
            List<TimetableSlot> existingSlots,
            CancellationToken cancellationToken = default)
        {
            if (teacherId == 0) return null;

            var daysOfWeek = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };
            var teacher = await _context.Teachers.FindAsync(teacherId);

            if (teacher == null) return null;

            foreach (var day in daysOfWeek)
            {
                for (int period = 1; period <= 8; period++)
                {
                    if (day == originalDay && period == originalPeriod)
                        continue;

                    if (existingSlots.Any(ts => ts.DayOfWeek == day && ts.Period == period))
                        continue;

                    if (!IsTeacherAvailable(teacher, day, period))
                        continue;

                    var conflictingSlots = await _context.TimetableSlots
                        .Include(ts => ts.Timetable)
                        .Where(ts => ts.TeacherId == teacherId &&
                                     ts.DayOfWeek == day &&
                                     ts.Period == period &&
                                     ts.ClassId != classId &&
                                     ts.Timetable.IsActive)
                        .AnyAsync(cancellationToken);

                    if (conflictingSlots)
                        continue;

                    return (day, period);
                }
            }

            return null;
        }

        #endregion
    }

   
}