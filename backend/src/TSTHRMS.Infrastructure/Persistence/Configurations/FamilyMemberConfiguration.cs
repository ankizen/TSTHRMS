using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.ToTable("FamilyMembers");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Relation).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(f => new { f.TenantId, f.EmployeeId });

        builder.HasOne(f => f.Employee)
            .WithMany()
            .HasForeignKey(f => f.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
