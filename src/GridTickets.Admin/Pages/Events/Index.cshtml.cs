using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Events;

public class EventRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string VenueCity { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
}

public class EventsIndexModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    public IList<EventRow> Events { get; private set; } = new List<EventRow>();
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public new int Page { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public string? Search { get; private set; }
    public string? StatusFilter { get; private set; }

    public EventsIndexModel(GridTicketsDbContext db) => _db = db;

    public async Task OnGetAsync(string? search, string? status, int page = 1)
    {
        Page = page < 1 ? 1 : page;
        Search = search;
        StatusFilter = status;

        var query = _db.Events
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.Venue)
            .Include(e => e.TicketTiers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Title.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EventStatus>(status, true, out var parsedStatus))
            query = query.Where(e => e.Status == parsedStatus);

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

        Events = await query
            .OrderByDescending(e => e.StartDate)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(e => new EventRow
            {
                Id = e.Id, Title = e.Title,
                CategoryName = e.Category.Name,
                VenueName = e.Venue.Name, VenueCity = e.Venue.City,
                StartDate = e.StartDate, Status = e.Status.ToString(),
                MinPrice = e.TicketTiers.Any() ? e.TicketTiers.Min(t => t.Price) : 0
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is not null)
        {
            ev.IsDeleted = true;
            ev.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
