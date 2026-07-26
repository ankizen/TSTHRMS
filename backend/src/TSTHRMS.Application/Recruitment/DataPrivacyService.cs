using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

public class DataPrivacyService(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    ICandidateContext candidateContext,
    IFileStorageService fileStorageService) : IDataPrivacyService
{
    public async Task<RequestDeletionResult> RequestDeletionAsync(CancellationToken cancellationToken = default)
    {
        var candidateId = candidateContext.CandidateId;
        if (candidateId is null)
        {
            return RequestDeletionResult.Failure("Not signed in as a candidate.");
        }

        var alreadyPending = await dbContext.CandidateDataDeletionRequests.AnyAsync(
            r => r.CandidateId == candidateId && r.Status == CandidateDataDeletionRequestStatus.Pending, cancellationToken);

        if (alreadyPending)
        {
            return RequestDeletionResult.Failure("A deletion request is already pending review.");
        }

        dbContext.CandidateDataDeletionRequests.Add(new CandidateDataDeletionRequest
        {
            CandidateId = candidateId.Value,
            RequestedAt = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return RequestDeletionResult.Success();
    }

    public async Task<CandidateDataDeletionRequestDto?> GetMyDeletionRequestAsync(CancellationToken cancellationToken = default)
    {
        var candidateId = candidateContext.CandidateId;
        if (candidateId is null)
        {
            return null;
        }

        return await dbContext.CandidateDataDeletionRequests.AsNoTracking()
            .Where(r => r.CandidateId == candidateId)
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => MapProjection(r))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CandidateDataDeletionRequestDto>> GetDeletionRequestsAsync(
        CandidateDataDeletionRequestStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CandidateDataDeletionRequests.AsNoTracking().AsQueryable();
        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        return await query
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => MapProjection(r))
            .ToListAsync(cancellationToken);
    }

    public async Task<DecideDeletionRequestResult> DecideDeletionRequestAsync(
        Guid requestId, DecideDeletionRequestRequest request, CancellationToken cancellationToken = default)
    {
        var deletionRequest = await dbContext.CandidateDataDeletionRequests
            .Include(r => r.Candidate)
            .ThenInclude(c => c!.Applications)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (deletionRequest is null || deletionRequest.Status != CandidateDataDeletionRequestStatus.Pending)
        {
            return DecideDeletionRequestResult.Failure("This request has already been decided.");
        }

        if (request.Approve && deletionRequest.Candidate!.Applications.Any(a => a.Stage == ApplicationStage.Hired))
        {
            return DecideDeletionRequestResult.Failure(
                "This candidate was hired - their data is now an active employment record and can't be erased through this flow.");
        }

        deletionRequest.Status = request.Approve
            ? CandidateDataDeletionRequestStatus.Approved
            : CandidateDataDeletionRequestStatus.Rejected;
        deletionRequest.HrDecisionNotes = request.Notes;
        deletionRequest.DecidedByUserId = currentUserService.UserId;
        deletionRequest.DecidedAt = DateTimeOffset.UtcNow;

        if (request.Approve)
        {
            await AnonymizeCandidateAsync(deletionRequest.Candidate!, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return DecideDeletionRequestResult.Success(new CandidateDataDeletionRequestDto(
            deletionRequest.Id, deletionRequest.CandidateId,
            $"{deletionRequest.Candidate!.FirstName} {deletionRequest.Candidate.LastName}",
            deletionRequest.Candidate.Email, deletionRequest.RequestedAt, deletionRequest.Status,
            deletionRequest.HrDecisionNotes, deletionRequest.DecidedAt));
    }

    public async Task<int> RunRetentionSweepAsync(CancellationToken cancellationToken = default)
    {
        var retentionDays = await dbContext.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantContext.TenantId)
            .Select(t => (int?)t.RejectedCandidateRetentionDays)
            .FirstOrDefaultAsync(cancellationToken);

        if (retentionDays is null)
        {
            return 0;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays.Value);

        var candidates = await dbContext.Candidates
            .Include(c => c.Applications)
            .Where(c => !c.IsAnonymized && !c.IsInTalentPool)
            .ToListAsync(cancellationToken);

        var anonymizedCount = 0;
        foreach (var candidate in candidates)
        {
            if (candidate.Applications.Count == 0
                || candidate.Applications.Any(a => a.Stage == ApplicationStage.Hired)
                || candidate.Applications.Any(a => a.Stage != ApplicationStage.Rejected))
            {
                continue;
            }

            var mostRecentRejection = candidate.Applications.Max(a => a.StageChangedAt);
            if (mostRecentRejection > cutoff)
            {
                continue;
            }

            await AnonymizeCandidateAsync(candidate, cancellationToken);
            anonymizedCount++;
        }

        if (anonymizedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return anonymizedCount;
    }

    /// <summary>Overwrites PII in place rather than deleting the row - Applications/Offers/
    /// ApplicationStageHistories referencing this candidate stay exactly as they are, for the
    /// same audit/reporting reasons every other pipeline record in this module is append-only.</summary>
    private async Task AnonymizeCandidateAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        if (candidate.IsAnonymized)
        {
            return;
        }

        if (candidate.ResumeDocumentId is { } resumeDocumentId)
        {
            var resumeDocument = await dbContext.Documents.FindAsync([resumeDocumentId], cancellationToken);
            if (resumeDocument is not null)
            {
                await fileStorageService.DeleteAsync(resumeDocument.StorageKey, cancellationToken);
                dbContext.Documents.Remove(resumeDocument);
            }

            candidate.ResumeDocumentId = null;
        }

        candidate.FirstName = "Redacted";
        candidate.LastName = "Candidate";
        candidate.Email = $"redacted-{candidate.Id:N}@anonymized.invalid";
        candidate.Phone = "0000000000";
        candidate.CurrentCtc = null;
        candidate.ExpectedCtc = null;
        candidate.NoticePeriodDays = null;
        candidate.IsAnonymized = true;
        candidate.AnonymizedAt = DateTimeOffset.UtcNow;
    }

    private static CandidateDataDeletionRequestDto MapProjection(CandidateDataDeletionRequest r) => new(
        r.Id, r.CandidateId, r.Candidate!.FirstName + " " + r.Candidate.LastName, r.Candidate.Email,
        r.RequestedAt, r.Status, r.HrDecisionNotes, r.DecidedAt);
}
