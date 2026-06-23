using GridTickets.Domain.Common;

namespace GridTickets.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
