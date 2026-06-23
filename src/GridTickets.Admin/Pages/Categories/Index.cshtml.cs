using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Categories;

public class CategoryRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CategoriesIndexModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    public IList<CategoryRow> Categories { get; private set; } = new List<CategoryRow>();
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public new int Page { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public string? Search { get; private set; }

    public CategoriesIndexModel(GridTicketsDbContext db) => _db = db;

    public async Task OnGetAsync(string? search, int page = 1)
    {
        Page = page < 1 ? 1 : page;
        Search = search;

        var query = _db.Categories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) || c.Slug.ToLower().Contains(term));
        }

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

        Categories = await query
            .OrderBy(c => c.Name)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new CategoryRow
            {
                Id = c.Id, Name = c.Name, Slug = c.Slug,
                Description = c.Description, CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is not null)
        {
            category.IsDeleted = true;
            category.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
