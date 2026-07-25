using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TSTHRMS.Domain.Recruitment;

namespace TSTHRMS.Infrastructure.Persistence.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("Offers");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Token).IsRequired().HasMaxLength(64);
        builder.HasIndex(o => o.Token).IsUnique();

        // At most one offer per application - a fresh negotiation attempt after a decline is a
        // new application/posting, not a second Offer row here.
        builder.HasIndex(o => new { o.TenantId, o.ApplicationId }).IsUnique();

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.DeclineReason).HasColumnType("text");

        builder.HasOne(o => o.Application)
            .WithMany()
            .HasForeignKey(o => o.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
