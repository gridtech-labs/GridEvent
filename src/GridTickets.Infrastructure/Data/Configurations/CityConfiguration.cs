using GridTickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridTickets.Infrastructure.Data.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.State).HasMaxLength(100).IsRequired();
        builder.Property(c => c.ImageUrl).HasMaxLength(500);

        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => new { c.IsDeleted, c.SortOrder });
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
