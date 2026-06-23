using GridTickets.Domain.Common;

namespace GridTickets.Domain.Entities;

public class Collection : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
