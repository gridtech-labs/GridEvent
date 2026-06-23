using System.ComponentModel.DataAnnotations;
using GridTickets.Domain.Entities;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridTickets.Admin.Pages.Collections;

public class CollectionInputModel
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

public class CollectionCreateModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    [BindProperty]
    public CollectionInputModel Input { get; set; } = new();

    public CollectionCreateModel(GridTicketsDbContext db) => _db = db;

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        _db.Collections.Add(new Collection
        {
            Name = Input.Name,
            Description = Input.Description,
            SortOrder = Input.SortOrder,
            IsActive = Input.IsActive
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("/Collections/Index");
    }
}
