using GridTickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridTickets.Infrastructure.Data.Configurations;

public class TicketTierConfiguration : IEntityTypeConfiguration<TicketTier>
{
    public void Configure(EntityTypeBuilder<TicketTier> builder)
    {
        builder.ToTable("ticket_tiers");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.Price).HasPrecision(18, 2).IsRequired();

        builder.Ignore(t => t.AvailableQuantity);

        builder.HasIndex(t => t.EventId);
        builder.HasIndex(t => t.IsDeleted);
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasOne(t => t.Event)
               .WithMany(e => e.TicketTiers)
               .HasForeignKey(t => t.EventId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
