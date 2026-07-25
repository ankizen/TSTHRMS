using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 6: a per-posting configurable test step (Build Notes: config lives on the job
/// opening, not the candidate) delivered via an anonymous tokenized link, since the Candidate
/// Portal login this PDF assumes doesn't exist yet (that's Slice 6).
/// </summary>
public class AssessmentService(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    IFrontendLinkBuilder frontendLinkBuilder,
    IEmailSender emailSender,
    ILogger<AssessmentService> logger) : IAssessmentService
{
    public async Task<TestConfigurationDto?> GetTestConfigurationAsync(
        Guid jobPostingId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessPostingAsync(jobPostingId, cancellationToken))
        {
            return null;
        }

        var posting = await dbContext.JobPostings.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == jobPostingId, cancellationToken);
        return posting is null ? null : MapConfig(posting);
    }

    public async Task<TestConfigurationDto?> ConfigureTestAsync(
        Guid jobPostingId, TestConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessPostingAsync(jobPostingId, cancellationToken))
        {
            return null;
        }

        var posting = await dbContext.JobPostings.FirstOrDefaultAsync(p => p.Id == jobPostingId, cancellationToken);
        if (posting is null)
        {
            return null;
        }

        posting.IsAssessmentEnabled = request.IsEnabled;
        posting.AssessmentType = request.Type;
        posting.AssessmentInstructions = request.Instructions;
        posting.AssessmentTimeLimitMinutes = request.TimeLimitMinutes;
        posting.AssessmentResponseWindowDays = request.ResponseWindowDays;
        posting.AssessmentPassThreshold = request.PassThreshold;
        posting.AssessmentRetakeCooldownMonths = request.RetakeCooldownMonths;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapConfig(posting);
    }

    public async Task<SendAssessmentResult> SendAssessmentAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.Applications
            .Include(a => a.Candidate)
            .Include(a => a.JobPosting)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application is null || !await CanAccessPostingAsync(application.JobPostingId, cancellationToken))
        {
            return SendAssessmentResult.Failure("Application not found.");
        }

        var posting = application.JobPosting!;
        if (!posting.IsAssessmentEnabled)
        {
            return SendAssessmentResult.Failure("This job posting doesn't have a test configured.");
        }

        var alreadySent = await dbContext.AssessmentSubmissions
            .AnyAsync(a => a.ApplicationId == applicationId, cancellationToken);
        if (alreadySent)
        {
            return SendAssessmentResult.Failure("A test has already been sent for this application.");
        }

        var now = DateTimeOffset.UtcNow;
        var submission = new AssessmentSubmission
        {
            ApplicationId = applicationId,
            Token = GenerateToken(),
            SentAt = now,
            DueAt = now.AddDays(posting.AssessmentResponseWindowDays),
        };
        dbContext.AssessmentSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SendAssessmentEmailAsync(application, posting, submission, cancellationToken);

        return SendAssessmentResult.Success(new AssessmentSummaryDto(
            submission.Id, posting.AssessmentType, submission.SentAt, submission.DueAt, null, null, null, null));
    }

    public async Task<AssessmentDetailDto?> GetDetailAsync(
        Guid assessmentSubmissionId, CancellationToken cancellationToken = default)
    {
        var submission = await dbContext.AssessmentSubmissions.AsNoTracking()
            .Include(a => a.Application)
            .ThenInclude(a => a!.JobPosting)
            .FirstOrDefaultAsync(a => a.Id == assessmentSubmissionId, cancellationToken);

        if (submission is null || !await CanAccessPostingAsync(submission.Application!.JobPostingId, cancellationToken))
        {
            return null;
        }

        return MapDetail(submission, submission.Application!.JobPosting!);
    }

    public async Task<AssessmentDetailDto?> ScoreAsync(
        Guid assessmentSubmissionId, ScoreAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var submission = await dbContext.AssessmentSubmissions
            .Include(a => a.Application)
            .ThenInclude(a => a!.JobPosting)
            .FirstOrDefaultAsync(a => a.Id == assessmentSubmissionId, cancellationToken);

        if (submission is null || !await CanAccessPostingAsync(submission.Application!.JobPostingId, cancellationToken))
        {
            return null;
        }

        var posting = submission.Application!.JobPosting!;
        var now = DateTimeOffset.UtcNow;

        submission.Score = request.Score;
        submission.Passed = request.Score >= posting.AssessmentPassThreshold;
        submission.ReviewerComments = request.Comments;
        submission.ReviewedByUserId = currentUserService.UserId ?? Guid.Empty;
        submission.ReviewedAt = now;
        submission.RetakeAllowedAfter = submission.Passed == true
            ? null
            : DateOnly.FromDateTime(now.UtcDateTime).AddMonths(posting.AssessmentRetakeCooldownMonths);

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapDetail(submission, posting);
    }

    public async Task<PublicAssessmentDto?> GetPublicAssessmentAsync(
        string token, CancellationToken cancellationToken = default)
    {
        var result = await dbContext.AssessmentSubmissions.AsNoTracking()
            .Where(a => a.Token == token)
            .Select(a => new
            {
                a.DueAt,
                a.SubmittedAt,
                JobTitle = a.Application!.JobPosting!.Title,
                a.Application.JobPosting.AssessmentType,
                a.Application.JobPosting.AssessmentInstructions,
                a.Application.JobPosting.AssessmentTimeLimitMinutes,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        return new PublicAssessmentDto(
            result.JobTitle, result.AssessmentType, result.AssessmentInstructions, result.AssessmentTimeLimitMinutes,
            result.DueAt, DateTimeOffset.UtcNow > result.DueAt, result.SubmittedAt is not null);
    }

    public async Task<bool> SubmitPublicAssessmentAsync(
        string token, PublicAssessmentSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var submission = await dbContext.AssessmentSubmissions.FirstOrDefaultAsync(a => a.Token == token, cancellationToken);
        if (submission is null || submission.SubmittedAt is not null || DateTimeOffset.UtcNow > submission.DueAt)
        {
            return false;
        }

        submission.SubmissionText = request.SubmissionText;
        submission.SubmittedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static TestConfigurationDto MapConfig(JobPosting posting) => new(
        posting.IsAssessmentEnabled, posting.AssessmentType, posting.AssessmentInstructions,
        posting.AssessmentTimeLimitMinutes, posting.AssessmentResponseWindowDays, posting.AssessmentPassThreshold,
        posting.AssessmentRetakeCooldownMonths);

    private static AssessmentDetailDto MapDetail(AssessmentSubmission submission, JobPosting posting) => new(
        submission.Id, submission.ApplicationId, posting.AssessmentType, posting.AssessmentInstructions,
        posting.AssessmentTimeLimitMinutes, submission.SentAt, submission.DueAt, submission.SubmittedAt,
        submission.SubmissionText, submission.SubmissionDocumentId, submission.Score, submission.Passed,
        submission.ReviewerComments, submission.RetakeAllowedAfter);

    private async Task SendAssessmentEmailAsync(
        JobApplication application, JobPosting posting, AssessmentSubmission submission,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantSlug = await dbContext.Tenants.AsNoTracking()
                .Where(t => t.Id == tenantContext.TenantId)
                .Select(t => t.Slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (tenantSlug is null)
            {
                return;
            }

            var link = frontendLinkBuilder.BuildCareerSiteAssessmentLink(tenantSlug, submission.Token);
            var candidate = application.Candidate!;

            var html = $"""
                <p>Hi {WebUtility.HtmlEncode(candidate.FirstName)},</p>
                <p>As a next step for <strong>{WebUtility.HtmlEncode(posting.Title)}</strong>, please complete a short
                assessment.</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">Start the assessment</a></p>
                <p>Please submit by {submission.DueAt:f}. Once you begin, you'll have
                {posting.AssessmentTimeLimitMinutes} minutes to complete it.</p>
                """;

            await emailSender.SendAsync(candidate.Email, $"Assessment for {posting.Title}", html, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send assessment email for application {ApplicationId}", application.Id);
        }
    }

    /// <summary>Section 14: same ownership scoping as ApplicantService/InterviewService - a
    /// Manager only reaches postings under their own requisitions.</summary>
    private async Task<bool> CanAccessPostingAsync(Guid jobPostingId, CancellationToken cancellationToken)
    {
        var raisedByUserId = await dbContext.JobPostings.AsNoTracking()
            .Where(p => p.Id == jobPostingId)
            .Select(p => (Guid?)p.JobRequisition!.RaisedByUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (raisedByUserId is null)
        {
            return false;
        }

        if (currentUserService.Roles.Contains(RoleNames.HRAdmin) || currentUserService.Roles.Contains(RoleNames.HRBP))
        {
            return true;
        }

        return raisedByUserId == (currentUserService.UserId ?? Guid.Empty);
    }
}
