using GridTickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridTickets.Infrastructure.Data.Configurations;

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Address).HasMaxLength(500).IsRequired();
        builder.Property(v => v.City).HasMaxLength(100).IsRequired();
        builder.Property(v => v.State).HasMaxLength(100).IsRequired();

        builder.HasIndex(v => v.IsDeleted);
        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
