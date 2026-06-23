using System.ComponentModel.DataAnnotations;
using GridTickets.Domain.Entities;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridTickets.Admin.Pages.Cities;

public class CityInputModel
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, 9999)]
    public int SortOrder { get; set; } = 0;
}

public class CityCreateModel : PageModel
{
    private readonly GridTicketsDbContext _db;
    private readonly IWebHostEnvironment _env;

    [BindProperty]
    public CityInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? CityImage { get; set; }

    public CityCreateModel(GridTicketsDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        if (CityImage is { Length: > 0 })
            Input.ImageUrl = await SaveImageAsync(CityImage);

        _db.Cities.Add(new City
        {
            Name = Input.Name, State = Input.State,
            ImageUrl = Input.ImageUrl, IsActive = Input.IsActive,
            SortOrder = Input.SortOrder
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("/Cities/Index");
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var dir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "cities");
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var path = Path.Combine(dir, fileName);
        using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"{Request.Scheme}://{Request.Host}/uploads/cities/{fileName}";
    }
}
