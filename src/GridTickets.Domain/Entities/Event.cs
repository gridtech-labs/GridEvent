using GridTickets.Domain.Common;
using GridTickets.Domain.Enums;

namespace GridTickets.Domain.Entities;

public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? BannerImageUrl { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;

    public Guid VenueId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? CollectionId { get; set; }

    // Navigation
    public Venue Venue { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Collection? Collection { get; set; }
    public ICollection<TicketTier> TicketTiers { get; set; } = new List<TicketTier>();
}
