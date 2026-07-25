using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

public interface IAssessmentService
{
    /// <summary>Null means the posting wasn't found or the caller lacks access (same
    /// requisition-ownership scoping as IApplicantService/IInterviewService).</summary>
    Task<TestConfigurationDto?> GetTestConfigurationAsync(Guid jobPostingId, CancellationToken cancellationToken = default);

    Task<TestConfigurationDto?> ConfigureTestAsync(
        Guid jobPostingId, TestConfigurationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fails if the posting's test isn't enabled, or one was already sent for this
    /// application - a genuine retake is a new application/posting later, not a resend.</summary>
    Task<SendAssessmentResult> SendAssessmentAsync(Guid applicationId, CancellationToken cancellationToken = default);

    Task<AssessmentDetailDto?> GetDetailAsync(Guid assessmentSubmissionId, CancellationToken cancellationToken = default);

    Task<AssessmentDetailDto?> ScoreAsync(
        Guid assessmentSubmissionId, ScoreAssessmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Anonymous - resolved by the opaque token alone, under the tenant the public
    /// career-site request was already resolved to.</summary>
    Task<PublicAssessmentDto?> GetPublicAssessmentAsync(string token, CancellationToken cancellationToken = default);

    Task<bool> SubmitPublicAssessmentAsync(
        string token, PublicAssessmentSubmissionRequest request, CancellationToken cancellationToken = default);
}
