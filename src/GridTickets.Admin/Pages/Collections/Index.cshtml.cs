using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Collections;

public class CollectionIndexModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    public IList<CollectionRow> Collections { get; private set; } = new List<CollectionRow>();

    [TempData] public string? Success { get; set; }

    public CollectionIndexModel(GridTicketsDbContext db) => _db = db;

    public async Task OnGetAsync()
    {
        Collections = await _db.Collections
            .AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CollectionRow(c.Id, c.Name, c.Description, c.SortOrder, c.IsActive))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var col = await _db.Collections.FindAsync(id);
        if (col is not null)
        {
            col.IsDeleted = true;
            col.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            Success = $"Collection '{col.Name}' deleted.";
        }
        return RedirectToPage();
    }
}

public record CollectionRow(Guid Id, string Name, string? Description, int SortOrder, bool IsActive);
