using GridTickets.Domain.Common;

namespace GridTickets.Domain.Entities;

public class Venue : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int Capacity { get; set; }

    // Navigation
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
