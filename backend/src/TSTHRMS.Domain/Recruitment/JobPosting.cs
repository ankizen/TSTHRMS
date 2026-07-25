using TSTHRMS.Domain.Common;
using TSTHRMS.Domain.Employees;
using TSTHRMS.Domain.Tenancy;

namespace TSTHRMS.Domain.Recruitment;

/// <summary>
/// Section 1: the public-facing listing, published from an approved <see cref="JobRequisition"/>
/// with one click (Section 2 - "no duplicate manual posting"). <see cref="Slug"/> gives each job
/// its own SEO-friendly URL.
/// </summary>
public class JobPosting : TenantScopedEntity
{
    public Guid JobRequisitionId { get; set; }
    public JobRequisition? JobRequisition { get; set; }

    public required string Title { get; set; }

    /// <summary>Unique per tenant - forms the public job URL (/careers/{tenantSlug}/{slug}).</summary>
    public required string Slug { get; set; }
    public required string Description { get; set; }
    public string? Department { get; set; }
    public string? Location { get; set; }
    public EmploymentType EmploymentType { get; set; }

    public Guid LegalEntityId { get; set; }
    public LegalEntity? LegalEntity { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
