using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Employees;
using TSTHRMS.Application.Employees.Dtos;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Documents;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Application.Recruitment;

public class OnboardingService(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IUserDirectory userDirectory,
    IEmployeeService employeeService) : IOnboardingService
{
    public async Task<ConvertToEmployeeResult> ConvertToEmployeeAsync(
        Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.Applications
            .Include(a => a.Candidate)
            .Include(a => a.JobPosting)
            .ThenInclude(p => p!.JobRequisition)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application is null)
        {
            return ConvertToEmployeeResult.Failure("Application not found.");
        }

        if (application.Stage != ApplicationStage.OfferAccepted)
        {
            return ConvertToEmployeeResult.Failure(
                "This application isn't at the Offer Accepted stage yet - nothing to convert.");
        }

        var candidate = application.Candidate!;
        var posting = application.JobPosting!;
        var requisition = posting.JobRequisition!;

        var latestOfferVersion = await dbContext.Offers.AsNoTracking()
            .Where(o => o.ApplicationId == applicationId)
            .SelectMany(o => o.Versions)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var bankDetails = await dbContext.PreboardingChecklistItems.AsNoTracking()
            .Where(i => i.ApplicationId == applicationId && i.TaskType == PreboardingTaskType.BankDetails)
            .Select(i => new { i.BankAccountNumber, i.BankIfscCode })
            .FirstOrDefaultAsync(cancellationToken);

        var reportingManagerId = await userDirectory.GetEmployeeIdForUserAsync(requisition.RaisedByUserId, cancellationToken);

        var request = new EmployeeWriteRequest(
            posting.LegalEntityId,
            posting.ProductId,
            candidate.FirstName,
            candidate.LastName,
            // Section 3's application form never asks for gender - PreferNotToSay is an honest
            // "not provided" signal, not a guess; HR can update it after conversion.
            Gender.PreferNotToSay,
            null,
            candidate.Email,
            candidate.Phone,
            null,
            null,
            null,
            null,
            null,
            bankDetails?.BankAccountNumber,
            bankDetails?.BankIfscCode,
            latestOfferVersion?.DateOfJoining ?? DateOnly.FromDateTime(DateTime.UtcNow),
            latestOfferVersion?.Designation,
            requisition.Grade,
            posting.Department,
            posting.Location,
            reportingManagerId,
            posting.EmploymentType,
            latestOfferVersion is null ? null : latestOfferVersion.AnnualCtc / 12m,
            null,
            null,
            null,
            null,
            null);

        var employeeDto = await employeeService.CreateAsync(request, cancellationToken);
        if (employeeDto is null)
        {
            return ConvertToEmployeeResult.Failure(
                "Couldn't create the employee record - it may be outside your assigned legal entity/product scope.");
        }

        var employee = await dbContext.Employees.FirstAsync(e => e.Id == employeeDto.Id, cancellationToken);
        employee.SourceApplicationId = applicationId;

        var now = DateTimeOffset.UtcNow;
        var fromStage = application.Stage;
        application.Stage = ApplicationStage.Hired;
        application.StageChangedAt = now;
        dbContext.ApplicationStageHistories.Add(new ApplicationStageHistory
        {
            ApplicationId = application.Id,
            FromStage = fromStage,
            ToStage = ApplicationStage.Hired,
            Reason = "Converted to employee",
            ChangedByUserId = currentUserService.UserId ?? Guid.Empty,
            ChangedAt = now,
        });

        await AttachPreboardingDocumentsAsync(applicationId, employee.Id, cancellationToken);
        CreateOnboardingChecklist(employee.Id, request.DateOfJoining);
        await MarkReferralBonusPayableAsync(candidate, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var finalEmployeeDto = await employeeService.GetByIdAsync(employee.Id, cancellationToken);
        return ConvertToEmployeeResult.Success(finalEmployeeDto!);
    }

    public async Task<IReadOnlyList<OnboardingChecklistItemDto>?> GetChecklistAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessEmployeeAsync(employeeId, cancellationToken))
        {
            return null;
        }

        var items = await dbContext.OnboardingChecklistItems.AsNoTracking()
            .Where(i => i.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var ownerIds = items.Where(i => i.OwnerUserId is not null).Select(i => i.OwnerUserId!.Value).Distinct().ToList();
        var displayNames = await userDirectory.GetDisplayNamesAsync(ownerIds, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return items.Select(i => Map(i, displayNames, today)).ToList();
    }

    public async Task<OnboardingChecklistItemDto?> UpdateItemAsync(
        Guid itemId, UpdateOnboardingItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.OnboardingChecklistItems.FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
        if (item is null || !await CanAccessEmployeeAsync(item.EmployeeId, cancellationToken))
        {
            return null;
        }

        if (request.OwnerUserId is not null)
        {
            item.OwnerUserId = request.OwnerUserId;
        }

        if (request.DueDate is not null)
        {
            item.DueDate = request.DueDate.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var displayNames = item.OwnerUserId is null
            ? new Dictionary<Guid, string>()
            : await userDirectory.GetDisplayNamesAsync([item.OwnerUserId.Value], cancellationToken);

        return Map(item, displayNames, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task<OnboardingChecklistItemDto?> CompleteItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.OnboardingChecklistItems.FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
        if (item is null || !await CanAccessEmployeeAsync(item.EmployeeId, cancellationToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        item.Status = OnboardingTaskStatus.Completed;
        item.CompletedAt = now;
        item.CompletedByUserId = currentUserService.UserId;

        if (item.TaskType == OnboardingTaskType.PolicyAcknowledgement)
        {
            // Reuse the same Core HR field Phase 1 already built for this, rather than tracking
            // the acknowledgement in two places.
            var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == item.EmployeeId, cancellationToken);
            if (employee is not null)
            {
                employee.PoshAcknowledgedAt = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var displayNames = item.OwnerUserId is null
            ? new Dictionary<Guid, string>()
            : await userDirectory.GetDisplayNamesAsync([item.OwnerUserId.Value], cancellationToken);

        return Map(item, displayNames, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    private async Task AttachPreboardingDocumentsAsync(Guid applicationId, Guid employeeId, CancellationToken cancellationToken)
    {
        var documentTasks = await dbContext.PreboardingChecklistItems.AsNoTracking()
            .Where(i => i.ApplicationId == applicationId && i.DocumentId != null)
            .Select(i => new { i.TaskType, i.DocumentId })
            .ToListAsync(cancellationToken);

        foreach (var task in documentTasks)
        {
            var category = task.TaskType switch
            {
                PreboardingTaskType.EducationCertificate => EmployeeDocumentCategory.EducationCertificate,
                PreboardingTaskType.IdentityProof => EmployeeDocumentCategory.IdentityProof,
                PreboardingTaskType.PreviousEmploymentRelievingLetter => EmployeeDocumentCategory.PreviousEmploymentLetter,
                _ => (EmployeeDocumentCategory?)null,
            };

            if (category is null)
            {
                continue;
            }

            dbContext.EmployeeDocuments.Add(new EmployeeDocument
            {
                EmployeeId = employeeId,
                Category = category.Value,
                DocumentId = task.DocumentId!.Value,
                Notes = "Submitted during pre-boarding - review and record the underlying details "
                    + "(degree/institution, ID type/number, or previous employer) once verified.",
            });
        }
    }

    /// <summary>Section 4: a referral's bonus becomes Payable the moment the referred candidate
    /// is actually hired - snapshotting Tenant.ReferralBonusAmount at that instant so a later
    /// change to the tenant-wide amount doesn't retroactively alter this payout.</summary>
    private async Task MarkReferralBonusPayableAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        if (candidate.Source != CandidateSource.Referral || candidate.ReferredByEmployeeId is null)
        {
            return;
        }

        var bonusAmount = await dbContext.Tenants.AsNoTracking()
            .Where(t => t.Id == candidate.TenantId)
            .Select(t => t.ReferralBonusAmount)
            .FirstOrDefaultAsync(cancellationToken);

        if (bonusAmount is null)
        {
            return;
        }

        candidate.ReferralBonusAmount = bonusAmount;
        candidate.ReferralBonusStatus = ReferralBonusStatus.Payable;
    }

    private void CreateOnboardingChecklist(Guid employeeId, DateOnly dateOfJoining)
    {
        foreach (var taskType in Enum.GetValues<OnboardingTaskType>())
        {
            dbContext.OnboardingChecklistItems.Add(new OnboardingChecklistItem
            {
                EmployeeId = employeeId,
                TaskType = taskType,
                DueDate = dateOfJoining,
            });
        }
    }

    private static OnboardingChecklistItemDto Map(
        OnboardingChecklistItem item, IReadOnlyDictionary<Guid, string> displayNames, DateOnly today) => new(
        item.Id, item.TaskType, item.OwnerUserId,
        item.OwnerUserId is null ? null : displayNames.GetValueOrDefault(item.OwnerUserId.Value),
        item.DueDate, item.Status, item.CompletedAt,
        item.Status == OnboardingTaskStatus.Pending && item.DueDate < today);

    /// <summary>HRAdmin/HRBP always; a Manager only if they're this employee's own
    /// ReportingManager - a different notion of "manager" than the requisition-ownership
    /// scoping used everywhere else in this module, since onboarding is about the Employee
    /// record now, not the pipeline.</summary>
    private async Task<bool> CanAccessEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (currentUserService.Roles.Contains(RoleNames.HRAdmin) || currentUserService.Roles.Contains(RoleNames.HRBP))
        {
            return await dbContext.Employees.AsNoTracking().AnyAsync(e => e.Id == employeeId, cancellationToken);
        }

        var reportingManagerId = await dbContext.Employees.AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => (Guid?)e.ReportingManagerId)
            .FirstOrDefaultAsync(cancellationToken);

        return reportingManagerId is not null && reportingManagerId == currentUserService.EmployeeId;
    }
}
