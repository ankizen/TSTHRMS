using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

public class PreboardingService(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    ICandidateContext candidateContext,
    IFileStorageService fileStorageService,
    IEmailSender emailSender,
    ILogger<PreboardingService> logger) : IPreboardingService
{
    private static readonly PreboardingTaskType[] DocumentTaskTypes =
    [
        PreboardingTaskType.EducationCertificate,
        PreboardingTaskType.IdentityProof,
        PreboardingTaskType.PreviousEmploymentRelievingLetter,
    ];

    public async Task CreateChecklistAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var alreadyExists = await dbContext.PreboardingChecklistItems
            .AnyAsync(i => i.ApplicationId == applicationId, cancellationToken);
        if (alreadyExists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var taskTypes = Enum.GetValues<PreboardingTaskType>();

        foreach (var taskType in taskTypes)
        {
            var isWelcomeCommunication = taskType == PreboardingTaskType.WelcomeCommunication;
            dbContext.PreboardingChecklistItems.Add(new PreboardingChecklistItem
            {
                ApplicationId = applicationId,
                TaskType = taskType,
                // Section 10: "welcome communication ... sent automatically" - sending it here
                // is what completes it, no separate action needed.
                Status = isWelcomeCommunication ? PreboardingTaskStatus.Completed : PreboardingTaskStatus.Pending,
                CompletedAt = isWelcomeCommunication ? now : null,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await SendWelcomeEmailAsync(applicationId, cancellationToken);
    }

    public async Task<IReadOnlyList<PreboardingChecklistItemDto>?> GetChecklistAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        var items = await dbContext.PreboardingChecklistItems.AsNoTracking()
            .Where(i => i.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        return items
            .Select(i => new PreboardingChecklistItemDto(
                i.Id, i.TaskType, i.Status, i.CompletedAt, i.DocumentId,
                MaskBankAccountNumber(i.BankAccountNumber), i.BankIfscCode))
            .ToList();
    }

    public async Task<PreboardingChecklistItemDto?> CompleteItAssetTaskAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        var item = await dbContext.PreboardingChecklistItems.FirstOrDefaultAsync(
            i => i.ApplicationId == applicationId && i.TaskType == PreboardingTaskType.ItAssetRequest, cancellationToken);

        if (item is null)
        {
            return null;
        }

        item.Status = PreboardingTaskStatus.Completed;
        item.CompletedAt = DateTimeOffset.UtcNow;
        item.CompletedByUserId = currentUserService.UserId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PreboardingChecklistItemDto(item.Id, item.TaskType, item.Status, item.CompletedAt, null, null, null);
    }

    public async Task<IReadOnlyList<MyPreboardingTaskDto>?> GetMyChecklistAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        if (!await OwnsApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        return await dbContext.PreboardingChecklistItems.AsNoTracking()
            .Where(i => i.ApplicationId == applicationId)
            .Select(i => new MyPreboardingTaskDto(i.TaskType, i.Status, i.CompletedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SubmitDocumentTaskAsync(
        Guid applicationId,
        PreboardingTaskType taskType,
        Stream resumeStream,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        if (!DocumentTaskTypes.Contains(taskType) || !await OwnsApplicationAsync(applicationId, cancellationToken))
        {
            return false;
        }

        var item = await dbContext.PreboardingChecklistItems.FirstOrDefaultAsync(
            i => i.ApplicationId == applicationId && i.TaskType == taskType, cancellationToken);
        if (item is null)
        {
            return false;
        }

        var validationError = DocumentValidation.Validate(sizeBytes, contentType);
        if (validationError is not null)
        {
            return false;
        }

        var document = await DocumentAttachmentHelper.SaveAndReplaceAsync(
            dbContext, fileStorageService, tenantContext.TenantId, item.DocumentId,
            resumeStream, fileName, contentType, sizeBytes, null, cancellationToken);

        item.Document = document;
        item.Status = PreboardingTaskStatus.Completed;
        item.CompletedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SubmitBankDetailsAsync(
        Guid applicationId, SubmitBankDetailsRequest request, CancellationToken cancellationToken = default)
    {
        if (!await OwnsApplicationAsync(applicationId, cancellationToken))
        {
            return false;
        }

        var item = await dbContext.PreboardingChecklistItems.FirstOrDefaultAsync(
            i => i.ApplicationId == applicationId && i.TaskType == PreboardingTaskType.BankDetails, cancellationToken);
        if (item is null)
        {
            return false;
        }

        item.BankAccountNumber = request.BankAccountNumber;
        item.BankIfscCode = request.BankIfscCode;
        item.Status = PreboardingTaskStatus.Completed;
        item.CompletedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? MaskBankAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber))
        {
            return null;
        }

        var visibleDigits = accountNumber.Length > 4 ? accountNumber[^4..] : accountNumber;
        return $"••••{visibleDigits}";
    }

    private async Task SendWelcomeEmailAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        try
        {
            var details = await dbContext.Applications.AsNoTracking()
                .Where(a => a.Id == applicationId)
                .Select(a => new { a.Candidate!.FirstName, a.Candidate.Email, JobTitle = a.JobPosting!.Title })
                .FirstOrDefaultAsync(cancellationToken);

            if (details is null)
            {
                return;
            }

            var html = $"""
                <p>Hi {System.Net.WebUtility.HtmlEncode(details.FirstName)},</p>
                <p>Welcome to the team! We're excited to have you join us as <strong>{System.Net.WebUtility.HtmlEncode(details.JobTitle)}</strong>.</p>
                <p>Before your first day, please sign in to the Candidate Portal to submit a few documents
                (education certificates, ID proof, and bank details) so everything is ready when you join.
                Your hiring manager will follow up separately with first-day instructions.</p>
                """;

            await emailSender.SendAsync(details.Email, $"Welcome aboard: {details.JobTitle}", html, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send pre-boarding welcome email for application {ApplicationId}", applicationId);
        }
    }

    private async Task<bool> OwnsApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var candidateId = candidateContext.CandidateId;
        if (candidateId is null)
        {
            return false;
        }

        return await dbContext.Applications.AsNoTracking()
            .AnyAsync(a => a.Id == applicationId && a.CandidateId == candidateId, cancellationToken);
    }

    /// <summary>Section 14: same ownership scoping as the other internal recruitment
    /// services - a Manager only reaches applications under their own requisitions.</summary>
    private async Task<bool> CanAccessApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var raisedByUserId = await dbContext.Applications.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => (Guid?)a.JobPosting!.JobRequisition!.RaisedByUserId)
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
