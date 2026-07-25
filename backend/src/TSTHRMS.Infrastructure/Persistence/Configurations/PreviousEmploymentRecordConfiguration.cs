using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class PreviousEmploymentRecordConfiguration : IEntityTypeConfiguration<PreviousEmploymentRecord>
{
    public void Configure(EntityTypeBuilder<PreviousEmploymentRecord> builder)
    {
        builder.ToTable("PreviousEmploymentRecords");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Designation).HasMaxLength(100);
        builder.Property(p => p.YearsOfExperience).HasPrecision(5, 2);
        builder.Property(p => p.ReasonForLeaving).HasMaxLength(500);
        builder.Property(p => p.PreviousUan).HasMaxLength(20);

        builder.HasIndex(p => new { p.TenantId, p.EmployeeId });

        builder.HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.RelievingLetterDocument)
            .WithMany()
            .HasForeignKey(p => p.RelievingLetterDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.LastSalarySlipDocument)
            .WithMany()
            .HasForeignKey(p => p.LastSalarySlipDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
