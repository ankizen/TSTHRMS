using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

public interface IInterviewService
{
    /// <summary>Null means the application wasn't found, or the caller (a Manager who didn't
    /// raise the requisition) doesn't have access to it - same ownership scoping as
    /// IApplicantService.</summary>
    Task<InterviewDto?> ScheduleAsync(
        Guid applicationId, ScheduleInterviewRequest request, CancellationToken cancellationToken = default);

    Task<InterviewDto?> RescheduleAsync(
        Guid interviewId, RescheduleInterviewRequest request, CancellationToken cancellationToken = default);

    Task<InterviewDto?> UpdateStatusAsync(
        Guid interviewId, UpdateInterviewStatusRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InterviewDto>?> GetForApplicationAsync(
        Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>Section 14's self-service Interviewer view - every interview the caller is
    /// assigned to as a panelist, regardless of which requisition it's under.</summary>
    Task<IReadOnlyList<MyInterviewDto>> GetMyInterviewsAsync(CancellationToken cancellationToken = default);

    /// <summary>Null means the caller isn't an assigned panelist for this interview, or they've
    /// already submitted (Build Notes: feedback is create-only, never edited).</summary>
    Task<InterviewScorecardDto?> SubmitScorecardAsync(
        Guid interviewId, SubmitScorecardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Picker list for assigning panelists when scheduling - any existing login, not
    /// filtered by role, since Section 14's Interviewer can be anyone.</summary>
    Task<IReadOnlyList<InterviewerCandidateDto>> GetInterviewerCandidatesAsync(CancellationToken cancellationToken = default);
}
