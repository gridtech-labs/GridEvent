using System.ComponentModel.DataAnnotations;
using GridTickets.Domain.Entities;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Venues;

public class VenueInputModel
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [Range(1, 1_000_000)]
    public int Capacity { get; set; }
}

public record CityCatalogItem(string Name, string State);

public class VenueCreateModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    [BindProperty]
    public VenueInputModel Input { get; set; } = new();

    public IList<CityCatalogItem> Cities { get; private set; } = new List<CityCatalogItem>();

    public VenueCreateModel(GridTicketsDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadCitiesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCitiesAsync();
            return Page();
        }

        _db.Venues.Add(new Venue
        {
            Name = Input.Name, Address = Input.Address,
            City = Input.City, State = Input.State, Capacity = Input.Capacity
        });
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
