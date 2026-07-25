using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Employees;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class IdentityDocumentConfiguration : IEntityTypeConfiguration<IdentityDocument>
{
    public void Configure(EntityTypeBuilder<IdentityDocument> builder)
    {
        builder.ToTable("IdentityDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Number).IsRequired().HasMaxLength(20);

        // At most one document of a given type per employee.
        builder.HasIndex(d => new { d.TenantId, d.EmployeeId, d.DocumentType }).IsUnique();

        builder.HasOne(d => d.Employee)
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.ProofDocument)
            .WithMany()
            .HasForeignKey(d => d.ProofDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
