using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.ToTable("JobPostings");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(220);
        builder.HasIndex(p => new { p.TenantId, p.Slug }).IsUnique();

        builder.Property(p => p.Description).IsRequired().HasColumnType("text");
        builder.Property(p => p.Department).HasMaxLength(100);
        builder.Property(p => p.Location).HasMaxLength(150);
        builder.Property(p => p.EmploymentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.AssessmentType).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.AssessmentInstructions).HasColumnType("text");

        builder.HasIndex(p => new { p.TenantId, p.IsPublished });

        builder.HasOne(p => p.LegalEntity)
            .WithMany()
            .HasForeignKey(p => p.LegalEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
