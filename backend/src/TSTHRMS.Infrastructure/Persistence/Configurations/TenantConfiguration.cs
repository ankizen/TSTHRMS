using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Tenancy;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.ReferralBonusAmount).HasPrecision(12, 2);
        builder.Property(t => t.OfferLetterTemplate).HasColumnType("text");

        // Explicit DB-level default (not just the C# property initializer) so existing tenant
        // rows get backfilled to a real retention window when this column is added, rather than
        // an empty-column default of 0 - which would make every already-rejected candidate
        // immediately eligible for anonymization on the very next sweep.
        builder.Property(t => t.RejectedCandidateRetentionDays).HasDefaultValue(180);
    }
}
