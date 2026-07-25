using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TSTHRMS.Api.Filters;
using TSTHRMS.Application.Common.Interfaces;
using TSTHRMS.Application.Recruitment;
using TSTHRMS.Application.Recruitment.Dtos;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Api.Controllers;

/// <summary>
/// Section 1: the public career site. Anonymous, resolved to a tenant by {tenantSlug} instead of
/// the JWT tenant_id claim - see ResolvePublicTenantAttribute and TenantContext.
/// </summary>
[ApiController]
[Route("api/public/{tenantSlug}")]
[AllowAnonymous]
[ResolvePublicTenant]
public class PublicCareerSiteController(
    ICareerSiteService careerSiteService, IApplicationDbContext dbContext, ITenantContext tenantContext) : ControllerBase
{
    private const long MaxResumeSizeBytes = 10 * 1024 * 1024;

    [HttpGet("company")]
    public async Task<ActionResult<PublicCompanyDto>> GetCompany(string tenantSlug, CancellationToken cancellationToken)
    {
        var name = await dbContext.Tenants
            .Where(t => t.Id == tenantContext.TenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return name is null ? NotFound() : Ok(new PublicCompanyDto(name));
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<IReadOnlyList<PublicJobListItemDto>>> GetJobs(
        string tenantSlug, [FromQuery] PublicJobFilter filter, CancellationToken cancellationToken)
    {
        var jobs = await careerSiteService.GetPublishedJobsAsync(filter, cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("jobs/{jobSlug}")]
    public async Task<ActionResult<PublicJobDetailDto>> GetJobBySlug(
        string tenantSlug, string jobSlug, CancellationToken cancellationToken)
    {
        var job = await careerSiteService.GetPublishedJobBySlugAsync(jobSlug, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("jobs/{jobSlug}/apply")]
    [RequestSizeLimit(MaxResumeSizeBytes)]
    public async Task<ActionResult<ApplyResult>> Apply(
        string tenantSlug, string jobSlug, [FromForm] PublicApplicationRequest request,
        IFormFile? resume, CancellationToken cancellationToken)
    {
        await using var stream = resume?.OpenReadStream();
        var result = await careerSiteService.ApplyAsync(
            jobSlug, request, CandidateSource.CareerSite, stream, resume?.FileName,
            resume?.ContentType, resume?.Length ?? 0, cancellationToken);

        return result.Succeeded ? Ok(result) : BadRequest(new { error = result.Error });
    }
}

public record PublicCompanyDto(string Name);
