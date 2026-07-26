using Microsoft.EntityFrameworkCore;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment.Dtos;

namespace TSTHRMS.Application.Recruitment;

public class TenantSettingsService(IApplicationDbContext dbContext, ITenantContext tenantContext) : ITenantSettingsService
{
    public async Task<TenantSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantContext.TenantId, cancellationToken);
        return Map(tenant.RejectedCandidateRetentionDays, tenant.ReferralBonusAmount, tenant.OfferLetterTemplate);
    }

    public async Task<TenantSettingsDto> UpdateAsync(
        UpdateTenantSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants.FirstAsync(t => t.Id == tenantContext.TenantId, cancellationToken);

        tenant.RejectedCandidateRetentionDays = request.RejectedCandidateRetentionDays < 1
            ? tenant.RejectedCandidateRetentionDays
            : request.RejectedCandidateRetentionDays;
        tenant.ReferralBonusAmount = request.ReferralBonusAmount;
        tenant.OfferLetterTemplate = string.IsNullOrWhiteSpace(request.OfferLetterTemplate) ? null : request.OfferLetterTemplate;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(tenant.RejectedCandidateRetentionDays, tenant.ReferralBonusAmount, tenant.OfferLetterTemplate);
    }

    private static TenantSettingsDto Map(int retentionDays, decimal? referralBonusAmount, string? offerLetterTemplate) =>
        new(retentionDays, referralBonusAmount, offerLetterTemplate);
}
