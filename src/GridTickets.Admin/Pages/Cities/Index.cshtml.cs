using GridTickets.Domain.Entities;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Cities;

public class CityRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CitiesIndexModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    public IList<CityRow> Cities { get; private set; } = new List<CityRow>();

    public CitiesIndexModel(GridTicketsDbContext db) => _db = db;

    public async Task OnGetAsync()
    {
        Cities = await _db.Cities
            .AsNoTracking()
            .IgnoreQueryFilters()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CityRow
            {
                Id = c.Id, Name = c.Name, State = c.State,
                ImageUrl = c.ImageUrl, IsActive = c.IsActive,
                SortOrder = c.SortOrder, CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var city = await _db.Cities.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (city is not null)
        {
            city.IsDeleted = true;
            city.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
