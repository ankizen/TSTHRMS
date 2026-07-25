using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 2's requisition -> approval -> publish flow. Slice 1 uses a single approval gate
/// (HRAdmin/HRBP) rather than the PDF's full HRBP -> Entity Head/Finance -> Final routing - see
/// the Phase 2 plan for why that's deferred rather than built now.
/// </summary>
public class JobRequisitionService(
    IApplicationDbContext dbContext,
    ISequenceGenerator sequenceGenerator,
    ICurrentUserService currentUserService) : IJobRequisitionService
{
    public async Task<IReadOnlyList<JobRequisitionListItemDto>> GetListAsync(
        RequisitionStatus? status, CancellationToken cancellationToken = default)
    {
        var query = ApplyOwnerScope(dbContext.JobRequisitions.AsNoTracking());

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new JobRequisitionListItemDto(
                r.Id, r.RequisitionCode, r.Title, r.LegalEntity!.Name, r.Product!.Name,
                r.Openings, r.Reason, r.Status,
                r.JobPosting != null, r.JobPosting != null && r.JobPosting.IsPublished, r.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<JobRequisitionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requisition = await ApplyOwnerScope(dbContext.JobRequisitions.AsNoTracking())
            .Include(r => r.Approvals)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return requisition is null ? null : await MapAsync(requisition, cancellationToken);
    }

    public async Task<JobRequisitionDto> CreateAsync(
        JobRequisitionWriteRequest request, CancellationToken cancellationToken = default)
    {
        var nextValue = await sequenceGenerator.NextAsync("JobRequisitionCode", cancellationToken);

        var requisition = new JobRequisition
        {
            RequisitionCode = $"REQ{nextValue:D6}",
            Title = request.Title,
            LegalEntityId = request.LegalEntityId,
            ProductId = request.ProductId,
            Grade = request.Grade,
            Department = request.Department,
            EmploymentType = request.EmploymentType,
            Openings = request.Openings,
            BudgetPerOpening = request.BudgetPerOpening,
            Reason = request.Reason,
            JustificationNotes = request.JustificationNotes,
            InterviewRoundCount = request.InterviewRoundCount,
            Status = RequisitionStatus.Draft,
            RaisedByUserId = currentUserService.UserId ?? Guid.Empty,
        };

        dbContext.JobRequisitions.Add(requisition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await MapAsync(requisition, cancellationToken))!;
    }

    public async Task<JobRequisitionDto?> UpdateAsync(
        Guid id, JobRequisitionWriteRequest request, CancellationToken cancellationToken = default)
    {
        var requisition = await FindOwnedAsync(id, cancellationToken);
        if (requisition is null || requisition.Status is not (RequisitionStatus.Draft or RequisitionStatus.Rejected))
        {
            return null;
        }

        requisition.Title = request.Title;
        requisition.LegalEntityId = request.LegalEntityId;
        requisition.ProductId = request.ProductId;
        requisition.Grade = request.Grade;
        requisition.Department = request.Department;
        requisition.EmploymentType = request.EmploymentType;
        requisition.Openings = request.Openings;
        requisition.BudgetPerOpening = request.BudgetPerOpening;
        requisition.Reason = request.Reason;
        requisition.JustificationNotes = request.JustificationNotes;
        requisition.InterviewRoundCount = request.InterviewRoundCount;
        requisition.Status = RequisitionStatus.Draft;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(requisition, cancellationToken);
    }

    public async Task<JobRequisitionDto?> SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requisition = await FindOwnedAsync(id, cancellationToken);
        if (requisition is null || requisition.Status != RequisitionStatus.Draft)
        {
            return null;
        }

        requisition.Status = RequisitionStatus.PendingApproval;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(requisition, cancellationToken);
    }

    public async Task<JobRequisitionDto?> DecideAsync(
        Guid id, RequisitionApprovalDecision decision, RequisitionDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var requisition = await dbContext.JobRequisitions.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (requisition is null || requisition.Status != RequisitionStatus.PendingApproval)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.RequisitionApprovals.Add(new RequisitionApproval
        {
            JobRequisitionId = requisition.Id,
            ApproverUserId = currentUserService.UserId ?? Guid.Empty,
            Decision = decision,
            Comment = request.Comment,
            DecidedAt = now,
        });

        requisition.Status = decision == RequisitionApprovalDecision.Approved
            ? RequisitionStatus.Approved
            : RequisitionStatus.Rejected;

        if (decision == RequisitionApprovalDecision.Approved)
        {
            requisition.ApprovedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(requisition, cancellationToken);
    }

    public async Task<JobRequisitionDto?> PutOnHoldAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requisition = await dbContext.JobRequisitions
            .Include(r => r.JobPosting)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (requisition is null || requisition.Status != RequisitionStatus.Approved)
        {
            return null;
        }

        requisition.Status = RequisitionStatus.OnHold;
        if (requisition.JobPosting is { IsPublished: true } posting)
        {
            posting.IsPublished = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(requisition, cancellationToken);
    }

    public async Task<JobRequisitionDto?> ResumeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requisition = await dbContext.JobRequisitions.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (requisition is null || requisition.Status != RequisitionStatus.OnHold)
        {
            return null;
        }

        requisition.Status = RequisitionStatus.Approved;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(requisition, cancellationToken);
    }

    public async Task<JobRequisitionDto?> CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requisition = await dbContext.JobRequisitions
            .Include(r => r.JobPosting)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (requisition is null || requisition.Status is not (RequisitionStatus.Approved or RequisitionStatus.OnHold))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        requisition.Status = RequisitionStatus.Closed;
        requisition.ClosedAt = now;

        if (requisition.JobPosting is { } posting)
        {
            posting.IsPublished = false;
            posting.ClosedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(requisition, cancellationToken);
    }

    public async Task<JobRequisitionDto?> PublishAsync(
        Guid id, PublishJobPostingRequest request, CancellationToken cancellationToken = default)
    {
        var requisition = await dbContext.JobRequisitions
            .Include(r => r.JobPosting)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (requisition is null || requisition.Status != RequisitionStatus.Approved)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        if (requisition.JobPosting is { } existingPosting)
        {
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                existingPosting.Description = request.Description;
            }

            if (request.Location is not null)
            {
                existingPosting.Location = request.Location;
            }

            existingPosting.IsPublished = true;
            existingPosting.PublishedAt = now;
            existingPosting.ClosedAt = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return null;
            }

            var slug = await GenerateUniqueSlugAsync(requisition.Title, cancellationToken);

            dbContext.JobPostings.Add(new JobPosting
            {
                JobRequisitionId = requisition.Id,
                Title = requisition.Title,
                Slug = slug,
                Description = request.Description,
                Department = requisition.Department,
                Location = request.Location,
                EmploymentType = requisition.EmploymentType,
                LegalEntityId = requisition.LegalEntityId,
                ProductId = requisition.ProductId,
                IsPublished = true,
                PublishedAt = now,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapAsync(requisition, cancellationToken);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = SlugGenerator.FromTitle(title);
        var slug = baseSlug;
        var suffix = 2;

        while (await dbContext.JobPostings.AnyAsync(p => p.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    /// <summary>A Manager who is neither HRAdmin nor HRBP only ever sees requisitions they
    /// raised themselves - mirrors EmployeeService.ApplyHrbpScope's shape for a different role.</summary>
    private IQueryable<JobRequisition> ApplyOwnerScope(IQueryable<JobRequisition> query)
    {
        if (currentUserService.Roles.Contains(RoleNames.HRAdmin) || currentUserService.Roles.Contains(RoleNames.HRBP))
        {
            return query;
        }

        var userId = currentUserService.UserId ?? Guid.Empty;
        return query.Where(r => r.RaisedByUserId == userId);
    }

    private async Task<JobRequisition?> FindOwnedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await ApplyOwnerScope(dbContext.JobRequisitions).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    private async Task<JobRequisitionDto?> MapAsync(JobRequisition requisition, CancellationToken cancellationToken)
    {
        var legalEntityName = requisition.LegalEntity?.Name
            ?? await dbContext.LegalEntities.Where(e => e.Id == requisition.LegalEntityId)
                .Select(e => e.Name).FirstOrDefaultAsync(cancellationToken) ?? "";

        var productName = requisition.Product?.Name
            ?? await dbContext.Products.Where(p => p.Id == requisition.ProductId)
                .Select(p => p.Name).FirstOrDefaultAsync(cancellationToken) ?? "";

        var approvals = requisition.Approvals.Count > 0
            ? requisition.Approvals
            : await dbContext.RequisitionApprovals.AsNoTracking()
                .Where(a => a.JobRequisitionId == requisition.Id)
                .ToListAsync(cancellationToken);

        var posting = requisition.JobPosting ?? await dbContext.JobPostings.AsNoTracking()
            .Where(p => p.JobRequisitionId == requisition.Id)
            .FirstOrDefaultAsync(cancellationToken);

        JobPostingDto? postingDto = null;
        if (posting is not null)
        {
            var applicationCount = await dbContext.Applications
                .CountAsync(a => a.JobPostingId == posting.Id, cancellationToken);

            postingDto = new JobPostingDto(
                posting.Id, posting.Title, posting.Slug, posting.Description, posting.Department,
                posting.Location, posting.EmploymentType, posting.IsPublished, posting.PublishedAt,
                posting.ClosedAt, applicationCount);
        }

        return new JobRequisitionDto(
            requisition.Id, requisition.RequisitionCode, requisition.Title, requisition.LegalEntityId,
            legalEntityName, requisition.ProductId, productName, requisition.Grade, requisition.Department,
            requisition.EmploymentType, requisition.Openings, requisition.BudgetPerOpening, requisition.Reason,
            requisition.JustificationNotes, requisition.InterviewRoundCount, requisition.Status,
            requisition.RaisedByUserId, requisition.ApprovedAt, requisition.ClosedAt, postingDto,
            approvals.Select(a => new RequisitionApprovalDto(a.Id, a.ApproverUserId, a.Decision, a.Comment, a.DecidedAt)).ToList());
    }
}
