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
    DateTimeOffset AppliedAt);

public record MoveApplicationStageRequest(ApplicationStage Stage, string? Reason);
