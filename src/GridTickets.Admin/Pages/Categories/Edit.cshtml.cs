using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Categories;

public class CategoryEditModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    [BindProperty]
    public CategoryInputModel Input { get; set; } = new();

    public CategoryEditModel(GridTicketsDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        Input = new CategoryInputModel
        {
            Name = category.Name, Slug = category.Slug, Description = category.Description
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!ModelState.IsValid) return Page();

        if (await _db.Categories.AnyAsync(c => c.Slug == Input.Slug && c.Id != id))
        {
            ModelState.AddModelError(nameof(Input.Slug), "This slug is already in use.");
            return Page();
        }

        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = Input.Name;
        category.Slug = Input.Slug.ToLowerInvariant();
        category.Description = Input.Description;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToPage("/Categories/Index");
    }
}
