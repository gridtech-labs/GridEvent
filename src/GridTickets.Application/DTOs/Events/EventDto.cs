using GridTickets.Application.DTOs.TicketTiers;

namespace GridTickets.Application.DTOs.Events;

public class EventSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string VenueCity { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public Guid? CollectionId { get; set; }
    public string? CollectionName { get; set; }
    public List<TicketTierDto> TicketTiers { get; set; } = new();
}

public class EventDto : EventSummaryDto
{
    public string Description { get; set; } = string.Empty;
    public Guid VenueId { get; set; }
    public string VenueAddress { get; set; } = string.Empty;
    public string VenueState { get; set; } = string.Empty;
    public int VenueCapacity { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? BannerImageUrl { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid VenueId { get; set; }
    public Guid CategoryId { get; set; }
}

public class UpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? BannerImageUrl { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid VenueId { get; set; }
    public Guid CategoryId { get; set; }
}
