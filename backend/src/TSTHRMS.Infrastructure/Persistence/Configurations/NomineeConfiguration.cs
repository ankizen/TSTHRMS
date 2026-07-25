using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class NomineeConfiguration : IEntityTypeConfiguration<Nominee>
{
    public void Configure(EntityTypeBuilder<Nominee> builder)
    {
        builder.ToTable("Nominees");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.NominationType).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Name).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Relation).IsRequired().HasMaxLength(100);
        builder.Property(n => n.SharePercentage).HasPrecision(5, 2);
        builder.Property(n => n.ContactNumber).HasMaxLength(30);

        builder.HasIndex(n => new { n.TenantId, n.EmployeeId, n.NominationType });

        builder.HasOne(n => n.Employee)
            .WithMany()
            .HasForeignKey(n => n.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.FamilyMember)
            .WithMany()
            .HasForeignKey(n => n.FamilyMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.ConsentDocument)
            .WithMany()
            .HasForeignKey(n => n.ConsentDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
