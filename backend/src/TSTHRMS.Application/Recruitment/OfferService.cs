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
/// Section 8: offer creation, internal approval, sending, and the candidate's accept/decline -
/// delivered via the same anonymous tokenized-link pattern as Slice 4's assessments, since
/// Candidate Portal login (Slice 6) doesn't exist yet.
/// </summary>
public class OfferService(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    IFrontendLinkBuilder frontendLinkBuilder,
    IEmailSender emailSender,
    ILogger<OfferService> logger) : IOfferService
{
    public async Task<OfferDto?> CreateAsync(
        Guid applicationId, CreateOrReviseOfferRequest request, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        var alreadyExists = await dbContext.Offers.AnyAsync(o => o.ApplicationId == applicationId, cancellationToken);
        if (alreadyExists)
        {
            return null;
        }

        var offer = new Offer
        {
            ApplicationId = applicationId,
            Token = GenerateToken(),
            Status = OfferStatus.Draft,
        };
        dbContext.Offers.Add(offer);

        offer.Versions.Add(await BuildVersionAsync(applicationId, 1, request, cancellationToken));

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(offer, cancellationToken);
    }

    public async Task<OfferDto?> ReviseAsync(
        Guid offerId, CreateOrReviseOfferRequest request, CancellationToken cancellationToken = default)
    {
        var offer = await dbContext.Offers
            .Include(o => o.Versions)
            .FirstOrDefaultAsync(o => o.Id == offerId, cancellationToken);

        if (offer is null || !await CanAccessApplicationAsync(offer.ApplicationId, cancellationToken)
            || offer.Status is OfferStatus.Accepted or OfferStatus.Declined)
        {
            return null;
        }

        var nextVersion = offer.Versions.Count + 1;
        offer.Versions.Add(await BuildVersionAsync(offer.ApplicationId, nextVersion, request, cancellationToken));
        offer.Status = OfferStatus.Draft;
        offer.ApprovedByUserId = null;
        offer.ApprovedAt = null;
        offer.SentAt = null;
        offer.ExpiresAt = null;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(offer, cancellationToken);
    }

    public async Task<OfferDto?> SubmitForApprovalAsync(Guid offerId, CancellationToken cancellationToken = default)
    {
        var offer = await dbContext.Offers.Include(o => o.Versions).FirstOrDefaultAsync(o => o.Id == offerId, cancellationToken);
        if (offer is null || !await CanAccessApplicationAsync(offer.ApplicationId, cancellationToken)
            || offer.Status != OfferStatus.Draft)
        {
            return null;
        }

        offer.Status = OfferStatus.PendingApproval;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(offer, cancellationToken);
    }

    public async Task<OfferDto?> ApproveAsync(
        Guid offerId, OfferDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var offer = await dbContext.Offers.Include(o => o.Versions).FirstOrDefaultAsync(o => o.Id == offerId, cancellationToken);
        if (offer is null || !await CanAccessApplicationAsync(offer.ApplicationId, cancellationToken)
            || offer.Status != OfferStatus.PendingApproval)
        {
            return null;
        }

        offer.Status = OfferStatus.Approved;
        offer.ApprovedByUserId = currentUserService.UserId ?? Guid.Empty;
        offer.ApprovedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(offer, cancellationToken);
    }

    public async Task<OfferDto?> SendAsync(
        Guid offerId, SendOfferRequest request, CancellationToken cancellationToken = default)
    {
        var offer = await dbContext.Offers.Include(o => o.Versions).FirstOrDefaultAsync(o => o.Id == offerId, cancellationToken);
        if (offer is null || !await CanAccessApplicationAsync(offer.ApplicationId, cancellationToken)
            || offer.Status != OfferStatus.Approved)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        offer.Status = OfferStatus.Sent;
        offer.SentAt = now;
        offer.ExpiresAt = now.AddDays(request.ResponseWindowDays);

        await dbContext.SaveChangesAsync(cancellationToken);

        await SendOfferEmailAsync(offer, cancellationToken);

        return await MapAsync(offer, cancellationToken);
    }

    public async Task<OfferDto?> GetForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        var offer = await dbContext.Offers
            .Include(o => o.Versions)
            .FirstOrDefaultAsync(o => o.ApplicationId == applicationId, cancellationToken);

        return offer is null ? null : await MapAsync(offer, cancellationToken);
    }

    public async Task<PublicOfferDto?> GetPublicOfferAsync(string token, CancellationToken cancellationToken = default)
    {
        var offer = await dbContext.Offers
            .Include(o => o.Versions)
            .Include(o => o.Application)
            .ThenInclude(a => a!.Candidate)
            .Include(o => o.Application)
            .ThenInclude(a => a!.JobPosting)
            .FirstOrDefaultAsync(o => o.Token == token, cancellationToken);

        if (offer is null)
        {
            return null;
        }

        await ExpireIfPastDueAsync(offer, cancellationToken);

        var latest = offer.Versions.OrderByDescending(v => v.VersionNumber).First();
        var candidate = offer.Application!.Candidate!;

        return new PublicOfferDto(
            $"{candidate.FirstName} {candidate.LastName}", offer.Application.JobPosting!.Title, latest.Designation,
            latest.DateOfJoining, latest.AnnualCtc, latest.FixedComponent, latest.VariableComponent,
            latest.JoiningBonus, latest.OfferLetterText, offer.ExpiresAt ?? DateTimeOffset.MaxValue,
            offer.Status == OfferStatus.Expired, offer.Status);
    }

    public async Task<bool> RespondPublicOfferAsync(
        string token, PublicOfferDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var offer = await dbContext.Offers
            .Include(o => o.Application)
            .FirstOrDefaultAsync(o => o.Token == token, cancellationToken);

        if (offer is null)
        {
            return false;
        }

        await ExpireIfPastDueAsync(offer, cancellationToken);
        if (offer.Status != OfferStatus.Sent)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        offer.RespondedAt = now;

        var application = offer.Application!;
        var fromStage = application.Stage;

        if (request.Accepted)
        {
            offer.Status = OfferStatus.Accepted;
            application.Stage = ApplicationStage.OfferAccepted;
            application.RejectionReason = null;
        }
        else
        {
            offer.Status = OfferStatus.Declined;
            offer.DeclineReason = request.DeclineReason;
            application.Stage = ApplicationStage.Rejected;
            application.RejectionReason = string.IsNullOrWhiteSpace(request.DeclineReason)
                ? "Candidate declined the offer."
                : $"Candidate declined the offer: {request.DeclineReason}";
        }

        application.StageChangedAt = now;
        dbContext.ApplicationStageHistories.Add(new ApplicationStageHistory
        {
            ApplicationId = application.Id,
            FromStage = fromStage,
            ToStage = application.Stage,
            Reason = request.Accepted ? "Offer accepted" : application.RejectionReason,
            ChangedByUserId = Guid.Empty,
            ChangedAt = now,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ExpireIfPastDueAsync(Offer offer, CancellationToken cancellationToken)
    {
        if (offer.Status == OfferStatus.Sent && offer.ExpiresAt is not null && DateTimeOffset.UtcNow > offer.ExpiresAt)
        {
            offer.Status = OfferStatus.Expired;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<OfferVersion> BuildVersionAsync(
        Guid applicationId, int versionNumber, CreateOrReviseOfferRequest request, CancellationToken cancellationToken)
    {
        var letterText = request.OfferLetterText;
        if (string.IsNullOrWhiteSpace(letterText))
        {
            var details = await dbContext.Applications.AsNoTracking()
                .Where(a => a.Id == applicationId)
                .Select(a => new { a.Candidate!.FirstName, a.Candidate.LastName, JobTitle = a.JobPosting!.Title })
                .FirstOrDefaultAsync(cancellationToken);

            letterText = BuildDefaultOfferLetterText(
                $"{details?.FirstName} {details?.LastName}", details?.JobTitle ?? "the role", request);
        }

        return new OfferVersion
        {
            VersionNumber = versionNumber,
            Designation = request.Designation,
            DateOfJoining = request.DateOfJoining,
            AnnualCtc = request.AnnualCtc,
            FixedComponent = request.FixedComponent,
            VariableComponent = request.VariableComponent,
            JoiningBonus = request.JoiningBonus,
            OfferLetterText = letterText,
            RevisionReason = request.RevisionReason,
            CreatedByUserId = currentUserService.UserId ?? Guid.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static string BuildDefaultOfferLetterText(string candidateName, string jobTitle, CreateOrReviseOfferRequest request) =>
        $"""
        Dear {candidateName},

        We are pleased to offer you the position of {request.Designation ?? jobTitle} with an annual CTC of
        {request.AnnualCtc:N0}, joining on {request.DateOfJoining:d MMMM yyyy}.

        We look forward to having you on the team.
        """;

    private async Task<OfferDto> MapAsync(Offer offer, CancellationToken cancellationToken)
    {
        await ExpireIfPastDueAsync(offer, cancellationToken);

        var versions = offer.Versions
            .OrderBy(v => v.VersionNumber)
            .Select(v => new OfferVersionDto(
                v.VersionNumber, v.Designation, v.DateOfJoining, v.AnnualCtc, v.FixedComponent, v.VariableComponent,
                v.JoiningBonus, v.OfferLetterText, v.RevisionReason, v.CreatedAt))
            .ToList();

        return new OfferDto(
            offer.Id, offer.ApplicationId, offer.Status, offer.SentAt, offer.ExpiresAt, offer.RespondedAt,
            offer.DeclineReason, offer.ApprovedByUserId, offer.ApprovedAt, versions);
    }

    private static string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private async Task SendOfferEmailAsync(Offer offer, CancellationToken cancellationToken)
    {
        try
        {
            var details = await dbContext.Applications.AsNoTracking()
                .Where(a => a.Id == offer.ApplicationId)
                .Select(a => new { a.Candidate!.FirstName, a.Candidate.Email, JobTitle = a.JobPosting!.Title })
                .FirstOrDefaultAsync(cancellationToken);

            if (details is null)
            {
                return;
            }

            var tenantSlug = await dbContext.Tenants.AsNoTracking()
                .Where(t => t.Id == tenantContext.TenantId)
                .Select(t => t.Slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (tenantSlug is null)
            {
                return;
            }

            var link = frontendLinkBuilder.BuildCareerSiteOfferLink(tenantSlug, offer.Token);

            var html = $"""
                <p>Hi {WebUtility.HtmlEncode(details.FirstName)},</p>
                <p>Congratulations! We'd like to extend an offer for <strong>{WebUtility.HtmlEncode(details.JobTitle)}</strong>.</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">View your offer and respond</a></p>
                <p>Please respond by {offer.ExpiresAt:f}.</p>
                """;

            await emailSender.SendAsync(details.Email, $"Offer: {details.JobTitle}", html, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send offer email for offer {OfferId}", offer.Id);
        }
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
