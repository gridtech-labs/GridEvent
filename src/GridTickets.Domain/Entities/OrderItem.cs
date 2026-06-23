using GridTickets.Domain.Common;

namespace GridTickets.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid TicketTierId { get; set; }
    public string TierName { get; set; } = string.Empty;   // denormalized
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;

    // Navigation
    public Order Order { get; set; } = null!;
    public TicketTier TicketTier { get; set; } = null!;
}
