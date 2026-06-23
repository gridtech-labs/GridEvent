using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Venues;

public class VenueRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VenuesIndexModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    public IList<VenueRow> Venues { get; private set; } = new List<VenueRow>();
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public new int Page { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public string? Search { get; private set; }

    public VenuesIndexModel(GridTicketsDbContext db) => _db = db;

    public async Task OnGetAsync(string? search, int page = 1)
    {
        Page = page < 1 ? 1 : page;
        Search = search;

        var query = _db.Venues.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(v => v.Name.ToLower().Contains(term) || v.City.ToLower().Contains(term));
        }

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

        Venues = await query
            .OrderBy(v => v.Name)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(v => new VenueRow
            {
                Id = v.Id, Name = v.Name, City = v.City,
                State = v.State, Capacity = v.Capacity, CreatedAt = v.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var venue = await _db.Venues.FindAsync(id);
        if (venue is not null)
        {
            venue.IsDeleted = true;
            venue.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
