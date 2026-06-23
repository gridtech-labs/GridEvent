using System.ComponentModel.DataAnnotations;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Cities;

public class CityEditModel : PageModel
{
    private readonly GridTicketsDbContext _db;
    private readonly IWebHostEnvironment _env;

    [BindProperty]
    public CityInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? CityImage { get; set; }

    public CityEditModel(GridTicketsDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var city = await _db.Cities.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (city is null) return NotFound();

        Input = new CityInputModel
        {
            Name = city.Name, State = city.State, ImageUrl = city.ImageUrl,
            IsActive = city.IsActive, SortOrder = city.SortOrder
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!ModelState.IsValid) return Page();

        if (CityImage is { Length: > 0 })
            Input.ImageUrl = await SaveImageAsync(CityImage);

        var city = await _db.Cities.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (city is null) return NotFound();

        city.Name = Input.Name;
        city.State = Input.State;
        city.ImageUrl = Input.ImageUrl;
        city.IsActive = Input.IsActive;
        city.SortOrder = Input.SortOrder;
        city.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "City saved.";
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
