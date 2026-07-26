using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// Section 11: the one-click Candidate -> Employee conversion and the Day-1 onboarding
/// checklist it creates. HRAdmin/HRBP only - unlike the rest of the internal recruitment
/// surface, a Manager can't trigger this themselves (it creates a real, payroll-relevant Core HR
/// record), though a Manager who is the new hire's reporting manager can still view/complete the
/// resulting checklist.
/// </summary>
public interface IOnboardingService
{
    /// <summary>Fails if the application isn't at Stage.OfferAccepted (already converted, or
    /// never reached that stage), or if the caller (an HRBP) is scoped outside the posting's
    /// legal entity/product. On success, Application.Stage becomes Hired.</summary>
    Task<ConvertToEmployeeResult> ConvertToEmployeeAsync(Guid applicationId, CancellationToken cancellationToken = default);

    /// <summary>Null means not found, or the caller lacks access (HRAdmin/HRBP always; a Manager
    /// only if they're this employee's own ReportingManager).</summary>
    Task<IReadOnlyList<OnboardingChecklistItemDto>?> GetChecklistAsync(
        Guid employeeId, CancellationToken cancellationToken = default);

    Task<OnboardingChecklistItemDto?> UpdateItemAsync(
        Guid itemId, UpdateOnboardingItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>Completing the PolicyAcknowledgement task also stamps
    /// Employee.PoshAcknowledgedAt, reusing that Core HR field instead of tracking the
    /// acknowledgement twice.</summary>
    Task<OnboardingChecklistItemDto?> CompleteItemAsync(Guid itemId, CancellationToken cancellationToken = default);
}
