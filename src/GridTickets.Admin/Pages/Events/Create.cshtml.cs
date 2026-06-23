using System.ComponentModel.DataAnnotations;
using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Events;

public class EventInputModel
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [MaxLength(500)]
    public string? BannerImageUrl { get; set; }

    public string Status { get; set; } = "Draft";

    [Required]
    public Guid VenueId { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public Guid? CollectionId { get; set; }
}

public class EventCreateModel : PageModel
{
    private readonly GridTicketsDbContext _db;
    private readonly IWebHostEnvironment _env;

    [BindProperty]
    public EventInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? BannerImage { get; set; }

    public IList<VenueSelectItem> Venues { get; private set; } = new List<VenueSelectItem>();
    public IList<CategorySelectItem> Categories { get; private set; } = new List<CategorySelectItem>();
    public IList<CollectionSelectItem> Collections { get; private set; } = new List<CollectionSelectItem>();

    public EventCreateModel(GridTicketsDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Input.StartDate = DateTime.Today;
        Input.EndDate = DateTime.Today;
        await LoadSelectListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        if (!Enum.TryParse<EventStatus>(Input.Status, true, out var status))
        {
            ModelState.AddModelError(nameof(Input.Status), "Invalid status.");
            await LoadSelectListsAsync();
            return Page();
        }

        if (Input.EndDate <= Input.StartDate)
        {
            ModelState.AddModelError(nameof(Input.EndDate), "End date must be after start date.");
            await LoadSelectListsAsync();
            return Page();
        }

        if (BannerImage is { Length: > 0 })
            Input.BannerImageUrl = await SaveImageAsync(BannerImage);

        _db.Events.Add(new Event
        {
            Title = Input.Title, Description = Input.Description,
            StartDate = Input.StartDate.ToUniversalTime(),
            EndDate = Input.EndDate.ToUniversalTime(),
            BannerImageUrl = Input.BannerImageUrl,
            Status = status, VenueId = Input.VenueId, CategoryId = Input.CategoryId,
            CollectionId = Input.CollectionId == Guid.Empty ? null : Input.CollectionId
        });
        await _db.SaveChangesAsync();
        return RedirectToPage("/Events/Index");
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "events");
        Directory.CreateDirectory(uploadsDir);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var path = Path.Combine(uploadsDir, fileName);
        using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"{Request.Scheme}://{Request.Host}/uploads/events/{fileName}";
    }

    private async Task LoadSelectListsAsync()
    {
        Venues = await _db.Venues.AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new VenueSelectItem { Id = v.Id, Name = $"{v.Name} — {v.City}" })
            .ToListAsync();

        Categories = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategorySelectItem { Id = c.Id, Name = c.Name })
            .ToListAsync();

        Collections = await _db.Collections.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CollectionSelectItem { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }
}

public class VenueSelectItem { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
public class CategorySelectItem { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
public class CollectionSelectItem { public Guid Id { get; set; } public string Name { get; set; } = string.Empty; }
