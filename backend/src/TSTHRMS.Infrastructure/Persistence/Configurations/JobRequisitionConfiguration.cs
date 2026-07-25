using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class JobRequisitionConfiguration : IEntityTypeConfiguration<JobRequisition>
{
    public void Configure(EntityTypeBuilder<JobRequisition> builder)
    {
        builder.ToTable("JobRequisitions");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequisitionCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(r => new { r.TenantId, r.RequisitionCode }).IsUnique();

        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Grade).HasMaxLength(50);
        builder.Property(r => r.Department).HasMaxLength(100);
        builder.Property(r => r.BudgetPerOpening).HasPrecision(12, 2);
        builder.Property(r => r.JustificationNotes).HasColumnType("text");

        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.EmploymentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(r => new { r.TenantId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.RaisedByUserId });

        builder.HasOne(r => r.LegalEntity)
            .WithMany()
            .HasForeignKey(r => r.LegalEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.JobPosting)
            .WithOne(p => p.JobRequisition)
            .HasForeignKey<JobPosting>(p => p.JobRequisitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
