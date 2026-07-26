using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 12: time-to-hire, source effectiveness, offer-to-joining ratio, requisition ageing.
/// Everything is pulled into memory as flat projections and aggregated in C# rather than via
/// EF LINQ groupby/date-math translated to SQL - at this data volume (per-tenant recruitment
/// activity, not the whole table) that's simpler and avoids relying on Pomelo's translation of
/// DateTimeOffset arithmetic, which is untested territory in this codebase.
/// </summary>
public class RecruitmentReportingService(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService) : IRecruitmentReportingService
{
    private const int StaleRequisitionThresholdDays = 30;

    private static readonly RequisitionStatus[] OpenStatuses =
    [
        RequisitionStatus.Draft, RequisitionStatus.PendingApproval, RequisitionStatus.Approved, RequisitionStatus.OnHold,
    ];

    public async Task<RecruitmentReportDto> GetReportAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var requisitions = await ApplyOwnerScope(dbContext.JobRequisitions.AsNoTracking())
            .Select(r => new { r.Id, r.RequisitionCode, r.Title, r.Status, r.Openings, r.CreatedAt })
            .ToListAsync(cancellationToken);

        var requisitionIds = requisitions.Select(r => r.Id).ToHashSet();

        var postings = await dbContext.JobPostings.AsNoTracking()
            .Where(p => requisitionIds.Contains(p.JobRequisitionId))
            .Select(p => new { p.Id, p.Title })
            .ToListAsync(cancellationToken);

        var postingIds = postings.Select(p => p.Id).ToHashSet();

        var applications = await dbContext.Applications.AsNoTracking()
            .Where(a => postingIds.Contains(a.JobPostingId))
            .Select(a => new { a.Id, a.JobPostingId, a.CandidateId, a.Stage, a.AppliedAt })
            .ToListAsync(cancellationToken);

        var applicationIds = applications.Select(a => a.Id).ToHashSet();

        var hiredAtByApplication = await dbContext.ApplicationStageHistories.AsNoTracking()
            .Where(h => applicationIds.Contains(h.ApplicationId) && h.ToStage == ApplicationStage.Hired)
            .Select(h => new { h.ApplicationId, h.ChangedAt })
            .ToListAsync(cancellationToken);

        var firstHiredAt = hiredAtByApplication
            .GroupBy(h => h.ApplicationId)
            .ToDictionary(g => g.Key, g => g.Min(h => h.ChangedAt));

        var candidateIds = applications.Select(a => a.CandidateId).Distinct().ToList();
        var candidateSources = await dbContext.Candidates.AsNoTracking()
            .Where(c => candidateIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Source })
            .ToDictionaryAsync(c => c.Id, c => c.Source, cancellationToken);

        var offerStatuses = await dbContext.Offers.AsNoTracking()
            .Where(o => applicationIds.Contains(o.ApplicationId))
            .Select(o => o.Status)
            .ToListAsync(cancellationToken);

        // ---- Requisition ageing ----
        var ageing = requisitions
            .Where(r => OpenStatuses.Contains(r.Status))
            .Select(r =>
            {
                var ageInDays = (int)(now - r.CreatedAt).TotalDays;
                return new RequisitionAgeingDto(
                    r.Id, r.RequisitionCode, r.Title, r.Status, r.Openings, ageInDays,
                    ageInDays > StaleRequisitionThresholdDays);
            })
            .OrderByDescending(r => r.AgeInDays)
            .ToList();

        // ---- Time-to-hire (Applied -> first reached Hired), broken down by posting ----
        var postingTitleById = postings.ToDictionary(p => p.Id, p => p.Title);
        var appliedAtByApplication = applications.ToDictionary(a => a.Id, a => new { a.JobPostingId, a.AppliedAt });

        var timeToHireSamples = firstHiredAt
            .Where(kv => appliedAtByApplication.ContainsKey(kv.Key))
            .Select(kv => new
            {
                JobPostingId = appliedAtByApplication[kv.Key].JobPostingId,
                Days = (kv.Value - appliedAtByApplication[kv.Key].AppliedAt).TotalDays,
            })
            .ToList();

        var timeToHireByPosting = timeToHireSamples
            .GroupBy(s => s.JobPostingId)
            .Select(g => new TimeToHireByPostingDto(
                g.Key, postingTitleById.GetValueOrDefault(g.Key, "(unknown posting)"),
                g.Count(), Math.Round(g.Average(s => s.Days), 1)))
            .OrderByDescending(t => t.Hires)
            .ToList();

        double? averageTimeToHireDays = timeToHireSamples.Count > 0
            ? Math.Round(timeToHireSamples.Average(s => s.Days), 1)
            : null;

        // ---- Source effectiveness ----
        var sourceEffectiveness = applications
            .GroupBy(a => candidateSources.GetValueOrDefault(a.CandidateId))
            .Select(g =>
            {
                var applicationCount = g.Count();
                var hireCount = g.Count(a => firstHiredAt.ContainsKey(a.Id));
                return new SourceEffectivenessDto(
                    g.Key, applicationCount, hireCount,
                    applicationCount > 0 ? Math.Round(100.0 * hireCount / applicationCount, 1) : 0);
            })
            .OrderByDescending(s => s.Applications)
            .ToList();

        // ---- Summary ----
        var offersSent = offerStatuses.Count(s => s is OfferStatus.Sent or OfferStatus.Accepted or OfferStatus.Declined or OfferStatus.Expired);
        var offersAccepted = offerStatuses.Count(s => s == OfferStatus.Accepted);
        var hiresLast30Days = firstHiredAt.Values.Count(hiredAt => (now - hiredAt).TotalDays <= 30);
        var activeApplications = applications.Count(a => a.Stage is not (ApplicationStage.Hired or ApplicationStage.Rejected));

        var summary = new RecruitmentReportSummaryDto(
            requisitions.Count(r => OpenStatuses.Contains(r.Status)),
            activeApplications,
            hiresLast30Days,
            averageTimeToHireDays,
            offersSent,
            offersAccepted,
            offersSent > 0 ? Math.Round(100.0 * offersAccepted / offersSent, 1) : null,
            offersSent > 0 ? Math.Round(100.0 * firstHiredAt.Count / offersSent, 1) : null);

        return new RecruitmentReportDto(summary, sourceEffectiveness, ageing, timeToHireByPosting);
    }

    /// <summary>Same shape as JobRequisitionService.ApplyOwnerScope.</summary>
    private IQueryable<JobRequisition> ApplyOwnerScope(IQueryable<JobRequisition> query)
    {
        if (currentUserService.Roles.Contains(RoleNames.HRAdmin) || currentUserService.Roles.Contains(RoleNames.HRBP))
        {
            return query;
        }

        var userId = currentUserService.UserId ?? Guid.Empty;
        return query.Where(r => r.RaisedByUserId == userId);
    }
}
