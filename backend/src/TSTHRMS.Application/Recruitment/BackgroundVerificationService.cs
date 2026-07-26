using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

public class BackgroundVerificationService(
    IApplicationDbContext dbContext, ICurrentUserService currentUserService) : IBackgroundVerificationService
{
    public async Task<BgvDto?> GetForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        var bgv = await dbContext.BackgroundVerifications.AsNoTracking()
            .FirstOrDefaultAsync(b => b.ApplicationId == applicationId, cancellationToken);

        return bgv is null ? new BgvDto(applicationId, BgvStatus.NotStarted, null, false, null, null, null) : Map(bgv);
    }

    public async Task<BgvDto?> InitiateAsync(
        Guid applicationId, InitiateBgvRequest request, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        var bgv = await dbContext.BackgroundVerifications
            .FirstOrDefaultAsync(b => b.ApplicationId == applicationId, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (bgv is null)
        {
            bgv = new BackgroundVerification { ApplicationId = applicationId };
            dbContext.BackgroundVerifications.Add(bgv);
        }

        bgv.Status = BgvStatus.Initiated;
        bgv.VendorReference = request.VendorReference;
        bgv.IsConditionalJoining = request.IsConditionalJoining;
        bgv.InitiatedAt = now;
        bgv.ClearedAt = null;
        bgv.DiscrepancyNotes = null;
        bgv.UpdatedByUserId = currentUserService.UserId;
        bgv.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(bgv);
    }

    public async Task<BgvDto?> UpdateStatusAsync(
        Guid applicationId, UpdateBgvStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessApplicationAsync(applicationId, cancellationToken))
        {
            return null;
        }

        var bgv = await dbContext.BackgroundVerifications
            .FirstOrDefaultAsync(b => b.ApplicationId == applicationId, cancellationToken);

        if (bgv is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        bgv.Status = request.Status;
        bgv.DiscrepancyNotes = request.Status == BgvStatus.DiscrepancyFound ? request.Notes : null;
        bgv.ClearedAt = request.Status == BgvStatus.Clear ? now : null;
        bgv.UpdatedByUserId = currentUserService.UserId;
        bgv.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(bgv);
    }

    private static BgvDto Map(BackgroundVerification bgv) => new(
        bgv.ApplicationId, bgv.Status, bgv.VendorReference, bgv.IsConditionalJoining,
        bgv.InitiatedAt, bgv.ClearedAt, bgv.DiscrepancyNotes);

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
