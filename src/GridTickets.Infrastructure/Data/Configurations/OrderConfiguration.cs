using GridTickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GridTickets.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasConversion<int>();
        builder.Property(o => o.SubTotal).HasPrecision(18, 2);
        builder.Property(o => o.BookingFee).HasPrecision(18, 2);
        builder.Property(o => o.GrandTotal).HasPrecision(18, 2);
        builder.Property(o => o.RazorpayOrderId).HasMaxLength(100);
        builder.Property(o => o.RazorpayPaymentId).HasMaxLength(100);
        builder.Property(o => o.BookingReference).HasMaxLength(50);
        builder.Property(o => o.CustomerName).HasMaxLength(200);
        builder.Property(o => o.CustomerEmail).HasMaxLength(200);
        builder.Property(o => o.CustomerPhone).HasMaxLength(20);

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.EventId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.ExpiresAt);
        builder.HasIndex(o => o.RazorpayOrderId);
        builder.HasIndex(o => o.BookingReference).IsUnique().HasFilter("\"BookingReference\" IS NOT NULL");
        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.HasOne(o => o.User)
               .WithMany()
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Event)
               .WithMany()
               .HasForeignKey(o => o.EventId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
