using GridTickets.Domain.Common;
using GridTickets.Domain.Enums;

namespace GridTickets.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal SubTotal { get; set; }
    public decimal BookingFee { get; set; }
    public decimal GrandTotal { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? BookingReference { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Billing info captured at checkout
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
