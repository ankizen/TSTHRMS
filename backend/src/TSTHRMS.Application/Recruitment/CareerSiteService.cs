using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

public class CareerSiteService(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IFileStorageService fileStorageService,
    IEmailSender emailSender,
    ILogger<CareerSiteService> logger) : ICareerSiteService
{
    public async Task<IReadOnlyList<PublicJobListItemDto>> GetPublishedJobsAsync(
        PublicJobFilter filter, CancellationToken cancellationToken = default)
    {
        var query = dbContext.JobPostings.AsNoTracking().Where(p => p.IsPublished);

        if (filter.LegalEntityId is not null)
        {
            query = query.Where(p => p.LegalEntityId == filter.LegalEntityId);
        }

        if (filter.ProductId is not null)
        {
            query = query.Where(p => p.ProductId == filter.ProductId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            query = query.Where(p => p.Location != null && p.Location.Contains(filter.Location));
        }

        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            query = query.Where(p => p.Department != null && p.Department.Contains(filter.Department));
        }

        return await query
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new PublicJobListItemDto(
                p.Slug, p.Title, p.Department, p.Location, p.EmploymentType,
                p.LegalEntity!.Name, p.Product!.Name, p.PublishedAt!.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicJobDetailDto?> GetPublishedJobBySlugAsync(
        string jobSlug, CancellationToken cancellationToken = default)
    {
        return await dbContext.JobPostings.AsNoTracking()
            .Where(p => p.Slug == jobSlug && p.IsPublished)
            .Select(p => new PublicJobDetailDto(
                p.Slug, p.Title, p.Description, p.Department, p.Location, p.EmploymentType,
                p.LegalEntity!.Name, p.Product!.Name, p.PublishedAt!.Value))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ApplyResult> ApplyAsync(
        string jobSlug,
        PublicApplicationRequest request,
        CandidateSource source,
        Stream? resumeStream,
        string? resumeFileName,
        string? resumeContentType,
        long resumeSizeBytes,
        CancellationToken cancellationToken = default)
    {
        if (!request.ConsentGiven)
        {
            return ApplyResult.Failure("Consent to store and process your application data is required.");
        }

        if (resumeStream is null || resumeSizeBytes == 0 || string.IsNullOrWhiteSpace(resumeFileName))
        {
            return ApplyResult.Failure("A resume file is required.");
        }

        var resumeError = DocumentValidation.Validate(resumeSizeBytes, resumeContentType ?? "");
        if (resumeError is not null)
        {
            return ApplyResult.Failure(resumeError);
        }

        var posting = await dbContext.JobPostings
            .FirstOrDefaultAsync(p => p.Slug == jobSlug && p.IsPublished, cancellationToken);

        if (posting is null)
        {
            return ApplyResult.Failure("This job posting is no longer available.");
        }

        var candidate = await dbContext.Candidates.FirstOrDefaultAsync(
            c => c.Email == request.Email && c.Phone == request.Phone, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var resumeDocument = await DocumentAttachmentHelper.SaveAndReplaceAsync(
            dbContext, fileStorageService, tenantContext.TenantId, candidate?.ResumeDocumentId,
            resumeStream, resumeFileName, resumeContentType ?? "application/octet-stream", resumeSizeBytes,
            null, cancellationToken);

        if (candidate is null)
        {
            candidate = new Candidate
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Source = source,
                ConsentGivenAt = now,
            };
            dbContext.Candidates.Add(candidate);
        }
        else
        {
            candidate.FirstName = request.FirstName;
            candidate.LastName = request.LastName;
            candidate.ConsentGivenAt = now;
        }

        candidate.CurrentCtc = request.CurrentCtc;
        candidate.ExpectedCtc = request.ExpectedCtc;
        candidate.NoticePeriodDays = request.NoticePeriodDays;
        candidate.ResumeDocument = resumeDocument;

        // Flush now so Candidate.Id exists for the duplicate-application check and the
        // Application row below - cheaper than a second round trip if we skip it and hit a
        // FK violation instead.
        await dbContext.SaveChangesAsync(cancellationToken);

        var alreadyApplied = await dbContext.Applications
            .AnyAsync(a => a.CandidateId == candidate.Id && a.JobPostingId == posting.Id, cancellationToken);

        if (alreadyApplied)
        {
            return ApplyResult.Failure("You've already applied to this position.");
        }

        var application = new JobApplication
        {
            CandidateId = candidate.Id,
            JobPostingId = posting.Id,
            Stage = ApplicationStage.Applied,
            StageChangedAt = now,
            AppliedAt = now,
        };
        dbContext.Applications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SendAcknowledgementEmailAsync(candidate, posting.Title, cancellationToken);

        return ApplyResult.Success(application.Id);
    }

    private async Task SendAcknowledgementEmailAsync(Candidate candidate, string jobTitle, CancellationToken cancellationToken)
    {
        try
        {
            var html = $"""
                <p>Hi {System.Net.WebUtility.HtmlEncode(candidate.FirstName)},</p>
                <p>Thanks for applying for <strong>{System.Net.WebUtility.HtmlEncode(jobTitle)}</strong>. We've received your
                application and our hiring team will review it shortly.</p>
                <p>We'll be in touch if your profile is a fit for the next step.</p>
                """;

            await emailSender.SendAsync(candidate.Email, $"Application received: {jobTitle}", html, cancellationToken);
        }
        catch (Exception ex)
        {
            // Best-effort: a flaky/unconfigured SMTP relay should never fail the application
            // submission itself - the candidate record and pipeline entry are already saved.
            logger.LogWarning(ex, "Failed to send application acknowledgement email to {Email}", candidate.Email);
        }
    }
}
