using System.ComponentModel.DataAnnotations;
using GridTickets.Domain.Entities;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Categories;

public class CategoryInputModel
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100), RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug must be lowercase letters, numbers, and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class CategoryCreateModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    [BindProperty]
    public CategoryInputModel Input { get; set; } = new();

    public CategoryCreateModel(GridTicketsDbContext db) => _db = db;

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        if (await _db.Categories.AnyAsync(c => c.Slug == Input.Slug))
        {
            ModelState.AddModelError(nameof(Input.Slug), "This slug is already in use.");
            return Page();
        }

        _db.Categories.Add(new Category
        {
            Name = Input.Name,
            Slug = Input.Slug.ToLowerInvariant(),
            Description = Input.Description
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("/Categories/Index");
    }
}
