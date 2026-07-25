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
