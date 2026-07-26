using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("Candidates");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(255);
        builder.Property(c => c.Phone).IsRequired().HasMaxLength(30);
        builder.Property(c => c.CurrentCtc).HasPrecision(12, 2);
        builder.Property(c => c.ExpectedCtc).HasPrecision(12, 2);
        builder.Property(c => c.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.ReferralBonusAmount).HasPrecision(12, 2);

        // Explicit string default (not just the C# property initializer) so existing candidate
        // rows get backfilled to a real enum name when this column is added - an empty-string
        // default would fail to parse back into the enum on the very next read.
        builder.Property(c => c.ReferralBonusStatus).HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(ReferralBonusStatus.NotApplicable);

        // Not unique: a candidate could plausibly reuse a phone across a rare shared-number
        // edge case, and the dedupe check in CareerSiteService already looks up by this pair
        // before deciding whether to reuse a row - the index exists for that lookup's speed,
        // not to enforce the constraint at the database layer.
        builder.HasIndex(c => new { c.TenantId, c.Email, c.Phone });

        builder.HasOne(c => c.ResumeDocument)
            .WithMany()
            .HasForeignKey(c => c.ResumeDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.ReferredByEmployee)
            .WithMany()
            .HasForeignKey(c => c.ReferredByEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
