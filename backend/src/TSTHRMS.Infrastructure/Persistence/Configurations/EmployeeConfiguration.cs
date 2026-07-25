using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(e => new { e.TenantId, e.EmployeeCode }).IsUnique();

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Gender).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.EmploymentType).HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PersonalEmail).HasMaxLength(255);
        builder.Property(e => e.PersonalPhone).HasMaxLength(30);
        builder.Property(e => e.CurrentAddress).HasColumnType("text");
        builder.Property(e => e.PermanentAddress).HasColumnType("text");
        builder.Property(e => e.EmergencyContactName).HasMaxLength(200);
        builder.Property(e => e.EmergencyContactRelation).HasMaxLength(50);
        builder.Property(e => e.EmergencyContactPhone).HasMaxLength(30);
        builder.Property(e => e.BankAccountNumber).HasMaxLength(50);
        builder.Property(e => e.BankIfscCode).HasMaxLength(20);
        builder.Property(e => e.Designation).HasMaxLength(100);
        builder.Property(e => e.Grade).HasMaxLength(50);
        builder.Property(e => e.Department).HasMaxLength(100);

        builder.Property(e => e.MonthlyGrossSalary).HasPrecision(10, 2);
        builder.Property(e => e.DateOfBirthProofType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.ProfessionalTaxState).HasMaxLength(100);

        builder.Property(e => e.ConfirmationStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => new { e.TenantId, e.LastName, e.FirstName });

        builder.HasOne(e => e.LegalEntity)
            .WithMany()
            .HasForeignKey(e => e.LegalEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReportingManager)
            .WithMany()
            .HasForeignKey(e => e.ReportingManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ConfirmingManager)
            .WithMany()
            .HasForeignKey(e => e.ConfirmingManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
