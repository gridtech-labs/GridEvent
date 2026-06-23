using GridTickets.Domain.Common;

namespace GridTickets.Domain.Entities;

public class TicketTier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; }

    public Guid EventId { get; set; }

    // Navigation
    public Event Event { get; set; } = null!;

    public int AvailableQuantity => TotalQuantity - SoldQuantity;
}
