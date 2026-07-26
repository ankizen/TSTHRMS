using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

/// <summary>
/// The "build now, toggle later" tenant-wide config this phase actually needed: rejected-candidate
/// retention (Section 13), referral bonus amount (Section 4), and the offer letter template
/// (Section 8). Job templates and multi-level approval routing - the other two items named in the
/// Phase 2 plan's deferred list - are deliberately NOT built here; see the Slice 10 commit message
/// for why.
/// </summary>
public interface ITenantSettingsService
{
    Task<TenantSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<TenantSettingsDto> UpdateAsync(UpdateTenantSettingsRequest request, CancellationToken cancellationToken = default);
}
