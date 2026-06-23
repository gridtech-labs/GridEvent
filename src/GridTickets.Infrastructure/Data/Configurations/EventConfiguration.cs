using GridTickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridTickets.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(5000).IsRequired();
        builder.Property(e => e.BannerImageUrl).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<int>();

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.StartDate);
        builder.HasIndex(e => e.IsDeleted);
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.Venue)
               .WithMany(v => v.Events)
               .HasForeignKey(e => e.VenueId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Category)
               .WithMany(c => c.Events)
               .HasForeignKey(e => e.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Collection)
               .WithMany(c => c.Events)
               .HasForeignKey(e => e.CollectionId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
