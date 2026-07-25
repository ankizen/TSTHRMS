using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class OfferVersionConfiguration : IEntityTypeConfiguration<OfferVersion>
{
    public void Configure(EntityTypeBuilder<OfferVersion> builder)
    {
        builder.ToTable("OfferVersions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Designation).HasMaxLength(100);
        builder.Property(v => v.AnnualCtc).HasPrecision(12, 2);
        builder.Property(v => v.FixedComponent).HasPrecision(12, 2);
        builder.Property(v => v.VariableComponent).HasPrecision(12, 2);
        builder.Property(v => v.JoiningBonus).HasPrecision(12, 2);
        builder.Property(v => v.OfferLetterText).HasColumnType("text");
        builder.Property(v => v.RevisionReason).HasMaxLength(1000);

        builder.HasIndex(v => new { v.TenantId, v.OfferId, v.VersionNumber }).IsUnique();

        builder.HasOne(v => v.Offer)
            .WithMany(o => o.Versions)
            .HasForeignKey(v => v.OfferId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
