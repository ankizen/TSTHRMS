using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment.Dtos;

// ---- Job Requisitions (internal) ----

public record JobRequisitionListItemDto(
    Guid Id,
    string RequisitionCode,
    string Title,
    string LegalEntityName,
    string ProductName,
    int Openings,
    RequisitionReason Reason,
    RequisitionStatus Status,
    bool HasJobPosting,
    bool IsPublished,
    DateTimeOffset CreatedAt);

public record JobRequisitionDto(
    Guid Id,
    string RequisitionCode,
    string Title,
    Guid LegalEntityId,
    string LegalEntityName,
    Guid ProductId,
    string ProductName,
    string? Grade,
    string? Department,
    EmploymentType EmploymentType,
    int Openings,
    decimal? BudgetPerOpening,
    RequisitionReason Reason,
    string? JustificationNotes,
    int InterviewRoundCount,
    RequisitionStatus Status,
    Guid RaisedByUserId,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? ClosedAt,
    JobPostingDto? JobPosting,
    IReadOnlyList<RequisitionApprovalDto> Approvals);

public record RequisitionApprovalDto(
    Guid Id, Guid ApproverUserId, RequisitionApprovalDecision Decision, string? Comment, DateTimeOffset DecidedAt);

public record JobRequisitionWriteRequest(
    string Title,
    Guid LegalEntityId,
    Guid ProductId,
    string? Grade,
    string? Department,
    EmploymentType EmploymentType,
    int Openings,
    decimal? BudgetPerOpening,
    RequisitionReason Reason,
    string? JustificationNotes,
    int InterviewRoundCount);

public record RequisitionDecisionRequest(string? Comment);

public record PublishJobPostingRequest(string? Description, string? Location);

// ---- Job Postings ----

public record JobPostingDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string? Department,
    string? Location,
    EmploymentType EmploymentType,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ClosedAt,
    int ApplicationCount);

// ---- Public career site ----

public record PublicJobFilter(Guid? LegalEntityId, Guid? ProductId, string? Location, string? Department);

public record PublicJobListItemDto(
    string Slug,
    string Title,
    string? Department,
    string? Location,
    EmploymentType EmploymentType,
    string LegalEntityName,
    string ProductName,
    DateTimeOffset PublishedAt);

public record PublicJobDetailDto(
    string Slug,
    string Title,
    string Description,
    string? Department,
    string? Location,
    EmploymentType EmploymentType,
    string LegalEntityName,
    string ProductName,
    DateTimeOffset PublishedAt);

public record PublicApplicationRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    decimal? CurrentCtc,
    decimal? ExpectedCtc,
    int? NoticePeriodDays,
    bool ConsentGiven);

public record ApplyResult(bool Succeeded, string? Error, Guid? ApplicationId)
{
    public static ApplyResult Success(Guid applicationId) => new(true, null, applicationId);
    public static ApplyResult Failure(string error) => new(false, error, null);
}

// ---- Applicant pipeline (internal) ----

public record ApplicantListItemDto(
    Guid ApplicationId,
    Guid CandidateId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    Guid? ResumeDocumentId,
    decimal? CurrentCtc,
    decimal? ExpectedCtc,
    int? NoticePeriodDays,
    CandidateSource Source,
    bool IsInTalentPool,
    ApplicationStage Stage,
    DateTimeOffset StageChangedAt,
    string? RejectionReason,
    DateTimeOffset AppliedAt,
    IReadOnlyList<CandidateOtherApplicationDto> OtherApplications,
    AssessmentSummaryDto? Assessment);

/// <summary>Section 3's duplicate detection, surfaced the other direction: when HR is looking
/// at one application, show the candidate's other in-flight applications so history isn't
/// missed just because they applied to a second role.</summary>
public record CandidateOtherApplicationDto(
    Guid ApplicationId, Guid JobPostingId, string JobPostingTitle, ApplicationStage Stage, DateTimeOffset AppliedAt);

public record MoveApplicationStageRequest(ApplicationStage Stage, string? Reason);

/// <summary>Section 5's "Keep in mind" list - a rejected-but-good candidate tagged for future
/// openings. Shows their most recent application so HR has context without re-opening the
/// original posting's pipeline.</summary>
public record TalentPoolCandidateDto(
    Guid CandidateId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    Guid? ResumeDocumentId,
    string? MostRecentJobPostingTitle,
    ApplicationStage? MostRecentStage,
    DateTimeOffset? MostRecentAppliedAt);

// ---- Interview Scheduling & Scorecards (Section 7) ----

public record ScheduleInterviewRequest(
    ApplicationStage Round, DateTimeOffset ScheduledAt, int DurationMinutes, string? VideoLink,
    IReadOnlyList<Guid> PanelistUserIds);

public record RescheduleInterviewRequest(DateTimeOffset ScheduledAt);

public record UpdateInterviewStatusRequest(InterviewStatus Status);

public record SubmitScorecardRequest(
    int TechnicalSkillsRating, int CommunicationRating, int ProblemSolvingRating, int CultureFitRating,
    InterviewRecommendation Recommendation, string? Comments);

public record InterviewPanelistDto(Guid UserId, string DisplayName, bool HasSubmitted);

/// <summary>Scorecard content is only ever populated in InterviewDto.Scorecards once every
/// assigned panelist has submitted (Section 7 - "avoids one interviewer influencing another"),
/// except the caller's own scorecard, which they can always see.</summary>
public record InterviewScorecardDto(
    Guid InterviewerUserId, string InterviewerDisplayName, int TechnicalSkillsRating, int CommunicationRating,
    int ProblemSolvingRating, int CultureFitRating, InterviewRecommendation Recommendation, string? Comments,
    DateTimeOffset SubmittedAt);

public record InterviewDto(
    Guid Id,
    Guid ApplicationId,
    ApplicationStage Round,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string? VideoLink,
    InterviewStatus Status,
    int RescheduleCount,
    IReadOnlyList<InterviewPanelistDto> Panelists,
    IReadOnlyList<InterviewScorecardDto> VisibleScorecards,
    bool AllScorecardsSubmitted,
    bool CurrentUserIsPanelist,
    bool CurrentUserHasSubmitted);

/// <summary>The Section 14 self-service "Interviewer" view - what's assigned to me, across every
/// requisition, regardless of whether I raised or can otherwise see the pipeline.</summary>
public record MyInterviewDto(
    Guid InterviewId,
    Guid ApplicationId,
    string CandidateName,
    string JobPostingTitle,
    ApplicationStage Round,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string? VideoLink,
    InterviewStatus Status,
    bool HasSubmitted);

public record InterviewerCandidateDto(Guid UserId, string Email, string? EmployeeName);

// ---- Assessment & Test Rounds (Section 6) ----

public record TestConfigurationRequest(
    bool IsEnabled,
    AssessmentType Type,
    string? Instructions,
    int TimeLimitMinutes,
    int ResponseWindowDays,
    int PassThreshold,
    int RetakeCooldownMonths);

public record TestConfigurationDto(
    bool IsEnabled,
    AssessmentType Type,
    string? Instructions,
    int TimeLimitMinutes,
    int ResponseWindowDays,
    int PassThreshold,
    int RetakeCooldownMonths);

/// <summary>The compact view shown alongside an applicant (Section 6 - "visible to interviewers
/// before the next round so they aren't re-asking what the test already covered").</summary>
public record AssessmentSummaryDto(
    Guid Id,
    AssessmentType Type,
    DateTimeOffset SentAt,
    DateTimeOffset DueAt,
    DateTimeOffset? SubmittedAt,
    int? Score,
    bool? Passed,
    DateOnly? RetakeAllowedAfter);

public record AssessmentDetailDto(
    Guid Id,
    Guid ApplicationId,
    AssessmentType Type,
    string? Instructions,
    int TimeLimitMinutes,
    DateTimeOffset SentAt,
    DateTimeOffset DueAt,
    DateTimeOffset? SubmittedAt,
    string? SubmissionText,
    Guid? SubmissionDocumentId,
    int? Score,
    bool? Passed,
    string? ReviewerComments,
    DateOnly? RetakeAllowedAfter);

public record ScoreAssessmentRequest(int Score, string? Comments);

public record SendAssessmentResult(bool Succeeded, string? Error, AssessmentSummaryDto? Assessment)
{
    public static SendAssessmentResult Success(AssessmentSummaryDto assessment) => new(true, null, assessment);
    public static SendAssessmentResult Failure(string error) => new(false, error, null);
}

public record PublicAssessmentDto(
    string JobTitle,
    AssessmentType Type,
    string? Instructions,
    int TimeLimitMinutes,
    DateTimeOffset DueAt,
    bool IsExpired,
    bool AlreadySubmitted);

public record PublicAssessmentSubmissionRequest(string SubmissionText);

// ---- Offer Management (Section 8) ----

public record OfferVersionDto(
    int VersionNumber,
    string? Designation,
    DateOnly DateOfJoining,
    decimal AnnualCtc,
    decimal? FixedComponent,
    decimal? VariableComponent,
    decimal? JoiningBonus,
    string? OfferLetterText,
    string? RevisionReason,
    DateTimeOffset CreatedAt);

public record OfferDto(
    Guid Id,
    Guid ApplicationId,
    OfferStatus Status,
    DateTimeOffset? SentAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RespondedAt,
    string? DeclineReason,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAt,
    IReadOnlyList<OfferVersionDto> Versions);

public record CreateOrReviseOfferRequest(
    string? Designation,
    DateOnly DateOfJoining,
    decimal AnnualCtc,
    decimal? FixedComponent,
    decimal? VariableComponent,
    decimal? JoiningBonus,
    string? OfferLetterText,
    string? RevisionReason);

public record OfferDecisionRequest(string? Comment);

public record SendOfferRequest(int ResponseWindowDays);

public record PublicOfferDto(
    string CandidateName,
    string JobTitle,
    string? Designation,
    DateOnly DateOfJoining,
    decimal AnnualCtc,
    decimal? FixedComponent,
    decimal? VariableComponent,
    decimal? JoiningBonus,
    string? OfferLetterText,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    OfferStatus Status);

public record PublicOfferDecisionRequest(bool Accepted, string? DeclineReason);

// ---- Candidate Portal (Section 3) ----

public record RequestCandidateOtpRequest(string Email);

public record VerifyCandidateOtpRequest(string Email, string Code);

public record CandidateLoginResultDto(bool Succeeded, string? AccessToken, DateTimeOffset? ExpiresAt, string? CandidateName);

public record MyApplicationInterviewDto(
    Guid InterviewId, ApplicationStage Round, DateTimeOffset ScheduledAt, int DurationMinutes,
    string? VideoLink, InterviewStatus Status);

/// <summary>Deliberately excludes Score/Passed - Section 14 keeps internal review outcomes out
/// of the candidate's own view, same principle as never showing them interview scorecards.</summary>
public record MyApplicationAssessmentDto(AssessmentType Type, DateTimeOffset DueAt, bool Submitted);

/// <summary>OfferToken is only populated when Status is Sent, so the frontend can link straight
/// to the existing accept/decline page without a second lookup.</summary>
public record MyApplicationOfferDto(OfferStatus Status, string? OfferToken);

public record MyApplicationDto(
    Guid ApplicationId,
    string JobPostingTitle,
    ApplicationStage Stage,
    DateTimeOffset AppliedAt,
    IReadOnlyList<MyApplicationInterviewDto> Interviews,
    MyApplicationAssessmentDto? Assessment,
    MyApplicationOfferDto? Offer);

// ---- Employee Referral Tracking (Section 4) ----

public record ReferralSubmissionRequest(string FirstName, string LastName, string Email, string Phone);

/// <summary>Stage only - Section 4: "referral status visible to the referring employee (without
/// exposing full interview feedback)".</summary>
public record MyReferralDto(
    Guid CandidateId, string CandidateName, string JobPostingTitle, ApplicationStage Stage, DateTimeOffset AppliedAt,
    ReferralBonusStatus ReferralBonusStatus, decimal? ReferralBonusAmount);

// ---- Background Verification (Section 9) ----

public record BgvDto(
    Guid ApplicationId,
    BgvStatus Status,
    string? VendorReference,
    bool IsConditionalJoining,
    DateTimeOffset? InitiatedAt,
    DateTimeOffset? ClearedAt,
    string? DiscrepancyNotes);

public record InitiateBgvRequest(string? VendorReference, bool IsConditionalJoining);

public record UpdateBgvStatusRequest(BgvStatus Status, string? Notes);

// ---- Pre-boarding (Section 10) ----

public record PreboardingChecklistItemDto(
    Guid Id,
    PreboardingTaskType TaskType,
    PreboardingTaskStatus Status,
    DateTimeOffset? CompletedAt,
    Guid? DocumentId,
    string? BankAccountNumberMasked,
    string? BankIfscCode);

public record SubmitBankDetailsRequest(string BankAccountNumber, string BankIfscCode);

/// <summary>The candidate's own pre-boarding view - same task shape as the internal one, minus
/// anything HR-internal (there's nothing hidden here, but kept as its own DTO so the two can
/// diverge later without a breaking change).</summary>
public record MyPreboardingTaskDto(PreboardingTaskType TaskType, PreboardingTaskStatus Status, DateTimeOffset? CompletedAt);

// ---- Onboarding Day-1 Conversion (Section 11) ----

public record ConvertToEmployeeResult(bool Succeeded, string? Error, EmployeeDto? Employee)
{
    public static ConvertToEmployeeResult Success(EmployeeDto employee) => new(true, null, employee);
    public static ConvertToEmployeeResult Failure(string error) => new(false, error, null);
}

public record OnboardingChecklistItemDto(
    Guid Id,
    OnboardingTaskType TaskType,
    Guid? OwnerUserId,
    string? OwnerDisplayName,
    DateOnly DueDate,
    OnboardingTaskStatus Status,
    DateTimeOffset? CompletedAt,
    bool IsOverdue);

public record UpdateOnboardingItemRequest(Guid? OwnerUserId, DateOnly? DueDate);

// ---- Recruitment Reporting & Analytics (Section 12) ----

public record RecruitmentReportSummaryDto(
    int OpenRequisitions,
    int ActiveApplications,
    int HiresLast30Days,
    double? AverageTimeToHireDays,
    int OffersSent,
    int OffersAccepted,
    double? OfferAcceptanceRatePercent,
    double? OfferToJoiningRatePercent);

public record SourceEffectivenessDto(
    CandidateSource Source,
    int Applications,
    int Hires,
    double ConversionRatePercent);

public record RequisitionAgeingDto(
    Guid RequisitionId,
    string RequisitionCode,
    string Title,
    RequisitionStatus Status,
    int Openings,
    int AgeInDays,
    bool IsStale);

public record TimeToHireByPostingDto(
    Guid JobPostingId,
    string Title,
    int Hires,
    double AverageTimeToHireDays);

public record RecruitmentReportDto(
    RecruitmentReportSummaryDto Summary,
    IReadOnlyList<SourceEffectivenessDto> SourceEffectiveness,
    IReadOnlyList<RequisitionAgeingDto> RequisitionAgeing,
    IReadOnlyList<TimeToHireByPostingDto> TimeToHireByPosting);

// ---- Recruitment Settings: retention, referral bonus, offer letter template (Sections 4, 8, 13) ----

public record TenantSettingsDto(int RejectedCandidateRetentionDays, decimal? ReferralBonusAmount, string? OfferLetterTemplate);

public record UpdateTenantSettingsRequest(int RejectedCandidateRetentionDays, decimal? ReferralBonusAmount, string? OfferLetterTemplate);

// ---- Referral Bonus Payouts (Section 4) ----

public record ReferralPayoutDto(
    Guid CandidateId,
    string CandidateName,
    string ReferredByEmployeeName,
    string JobPostingTitle,
    decimal BonusAmount,
    ReferralBonusStatus Status,
    DateTimeOffset? PaidAt);

// ---- Candidate Data Privacy / DPDPA (Section 13) ----

public record CandidateDataDeletionRequestDto(
    Guid Id,
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    DateTimeOffset RequestedAt,
    CandidateDataDeletionRequestStatus Status,
    string? HrDecisionNotes,
    DateTimeOffset? DecidedAt);

public record RequestDeletionResult(bool Succeeded, string? Error)
{
    public static RequestDeletionResult Success() => new(true, null);
    public static RequestDeletionResult Failure(string error) => new(false, error);
}

public record DecideDeletionRequestRequest(bool Approve, string? Notes);

public record DecideDeletionRequestResult(bool Succeeded, string? Error, CandidateDataDeletionRequestDto? Request)
{
    public static DecideDeletionRequestResult Success(CandidateDataDeletionRequestDto request) => new(true, null, request);
    public static DecideDeletionRequestResult Failure(string error) => new(false, error, null);
}
