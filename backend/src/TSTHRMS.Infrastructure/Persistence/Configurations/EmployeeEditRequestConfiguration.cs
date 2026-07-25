using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class EmployeeEditRequestConfiguration : IEntityTypeConfiguration<EmployeeEditRequest>
{
    public void Configure(EntityTypeBuilder<EmployeeEditRequest> builder)
    {
        builder.ToTable("EmployeeEditRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Field).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.OldValue).HasMaxLength(500);
        builder.Property(r => r.NewValue).IsRequired().HasMaxLength(500);
        builder.Property(r => r.ReviewNote).HasMaxLength(500);

        builder.HasIndex(r => new { r.TenantId, r.EmployeeId });
        builder.HasIndex(r => new { r.TenantId, r.Status });

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
