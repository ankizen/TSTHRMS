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

        // Not unique: "at most one active document of a given type per employee" is enforced in
        // IdentityDocumentService.CreateAsync instead, since a soft-deleted row (IsDeleted=true)
        // would otherwise still occupy the slot - MySQL has no partial/filtered unique index.
        builder.HasIndex(d => new { d.TenantId, d.EmployeeId, d.DocumentType });

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
