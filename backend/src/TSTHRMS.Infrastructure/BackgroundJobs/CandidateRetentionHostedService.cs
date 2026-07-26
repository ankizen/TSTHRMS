using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Domain.Tenancy;
using TSTHRMS.Infrastructure.Persistence;

namespace TSTHRMS.Infrastructure.BackgroundJobs;

/// <summary>
/// Section 13 (DPDPA 2023): once a day, anonymizes every active tenant's Rejected candidates who
/// have aged out past that tenant's retention window. The first IHostedService in this codebase -
/// it runs outside any HTTP request, so it constructs its own per-tenant ApplicationDbContext and
/// DataPrivacyService directly (StaticTenantContext/SystemCurrentUserService/NullCandidateContext)
/// instead of resolving the request-scoped Api.Services.TenantContext, which has no HttpContext to
/// read here.
/// </summary>
public class CandidateRetentionHostedService(
    IServiceScopeFactory scopeFactory, ILogger<CandidateRetentionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunSweepForAllTenantsAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunSweepForAllTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
        var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        List<Guid> tenantIds;
        await using (var lookupContext = new ApplicationDbContext(
            options, new StaticTenantContext(Guid.Empty), new SystemCurrentUserService()))
        {
            tenantIds = await lookupContext.Tenants
                .Where(t => t.Status == TenantStatus.Active)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);
        }

        foreach (var tenantId in tenantIds)
        {
            try
            {
                await using var context = new ApplicationDbContext(
                    options, new StaticTenantContext(tenantId), new SystemCurrentUserService());
                var privacyService = new DataPrivacyService(
                    context, new StaticTenantContext(tenantId), new SystemCurrentUserService(),
                    new NullCandidateContext(), fileStorageService);

                var anonymized = await privacyService.RunRetentionSweepAsync(cancellationToken);
                if (anonymized > 0)
                {
                    logger.LogInformation(
                        "Candidate retention sweep anonymized {Count} candidate(s) for tenant {TenantId}.",
                        anonymized, tenantId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Candidate retention sweep failed for tenant {TenantId}.", tenantId);
            }
        }
    }
}
