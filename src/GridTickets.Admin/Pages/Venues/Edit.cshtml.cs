using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Venues;

public class VenueEditModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    [BindProperty]
    public VenueInputModel Input { get; set; } = new();

    public IList<CityCatalogItem> Cities { get; private set; } = new List<CityCatalogItem>();

    public VenueEditModel(GridTicketsDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var venue = await _db.Venues.FindAsync(id);
        if (venue is null) return NotFound();

        Input = new VenueInputModel
        {
            Name = venue.Name, Address = venue.Address,
            City = venue.City, State = venue.State, Capacity = venue.Capacity
        };
        await LoadCitiesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            await LoadCitiesAsync();
            return Page();
        }

        var venue = await _db.Venues.FindAsync(id);
        if (venue is null) return NotFound();

        venue.Name = Input.Name;
        venue.Address = Input.Address;
        venue.City = Input.City;
        venue.State = Input.State;
        venue.Capacity = Input.Capacity;
        venue.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToPage("/Venues/Index");
    }

    private async Task LoadCitiesAsync()
    {
        Cities = await _db.Cities
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CityCatalogItem(c.Name, c.State))
            .ToListAsync();
    }
}
