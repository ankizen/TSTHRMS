using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class EducationRecordConfiguration : IEntityTypeConfiguration<EducationRecord>
{
    public void Configure(EntityTypeBuilder<EducationRecord> builder)
    {
        builder.ToTable("EducationRecords");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.QualificationLevel).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.VerificationStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.DegreeName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.InstituteName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Specialization).HasMaxLength(200);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId });

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CertificateDocument)
            .WithMany()
            .HasForeignKey(e => e.CertificateDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
