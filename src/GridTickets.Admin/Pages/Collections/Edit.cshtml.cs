using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridTickets.Admin.Pages.Collections;

public class CollectionEditModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    [BindProperty]
    public CollectionInputModel Input { get; set; } = new();

    public CollectionEditModel(GridTicketsDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var col = await _db.Collections.FindAsync(id);
        if (col is null) return NotFound();

        Input = new CollectionInputModel
        {
            Name = col.Name,
            Description = col.Description,
            SortOrder = col.SortOrder,
            IsActive = col.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!ModelState.IsValid) return Page();

        var col = await _db.Collections.FindAsync(id);
        if (col is null) return NotFound();

        col.Name = Input.Name;
        col.Description = Input.Description;
        col.SortOrder = Input.SortOrder;
        col.IsActive = Input.IsActive;
        col.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToPage("/Collections/Index");
    }
}
