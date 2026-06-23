using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Events;

public class TierRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}

public class EventEditModel : PageModel
{
    private readonly GridTicketsDbContext _db;
    private readonly IWebHostEnvironment _env;

    [BindProperty]
    public EventInputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? BannerImage { get; set; }

    public IList<TierRow> Tiers { get; private set; } = new List<TierRow>();
    public IList<VenueSelectItem> Venues { get; private set; } = new List<VenueSelectItem>();
    public IList<CategorySelectItem> Categories { get; private set; } = new List<CategorySelectItem>();
    public IList<CollectionSelectItem> Collections { get; private set; } = new List<CollectionSelectItem>();

    public EventEditModel(GridTicketsDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var ev = await _db.Events
            .Include(e => e.TicketTiers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev is null) return NotFound();

        Input = new EventInputModel
        {
            Title = ev.Title, Description = ev.Description,
            StartDate = ev.StartDate.ToLocalTime(),
            EndDate = ev.EndDate.ToLocalTime(),
            BannerImageUrl = ev.BannerImageUrl,
            Status = ev.Status.ToString(),
            VenueId = ev.VenueId, CategoryId = ev.CategoryId,
            CollectionId = ev.CollectionId
        };

        Tiers = ev.TicketTiers.Select(t => new TierRow
        {
            Id = t.Id, Name = t.Name, Description = t.Description,
            Price = t.Price, TotalQuantity = t.TotalQuantity,
            SoldQuantity = t.SoldQuantity, AvailableQuantity = t.AvailableQuantity
        }).OrderBy(t => t.Price).ToList();

        await LoadSelectListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveEventAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            await LoadTiersAsync(id);
            await LoadSelectListsAsync();
            return Page();
        }

        if (!Enum.TryParse<EventStatus>(Input.Status, true, out var status))
        {
            ModelState.AddModelError(nameof(Input.Status), "Invalid status.");
            await LoadTiersAsync(id);
            await LoadSelectListsAsync();
            return Page();
        }

        if (BannerImage is { Length: > 0 })
            Input.BannerImageUrl = await SaveImageAsync(BannerImage);

        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();

        ev.Title = Input.Title;
        ev.Description = Input.Description;
        ev.StartDate = Input.StartDate.ToUniversalTime();
        ev.EndDate = Input.EndDate.ToUniversalTime();
        ev.BannerImageUrl = Input.BannerImageUrl;
        ev.Status = status;
        ev.VenueId = Input.VenueId;
        ev.CategoryId = Input.CategoryId;
        ev.CollectionId = Input.CollectionId == Guid.Empty ? null : Input.CollectionId;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Event saved.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddTierAsync(
        Guid id, string tierName, string? tierDescription, decimal tierPrice, int tierQuantity)
    {
        _db.TicketTiers.Add(new TicketTier
        {
            Name = tierName, Description = tierDescription,
            Price = tierPrice, TotalQuantity = tierQuantity, EventId = id
        });
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostEditTierAsync(
        Guid id, Guid tierId, string tierName, string? tierDescription, decimal tierPrice, int tierQuantity)
    {
        var tier = await _db.TicketTiers.FirstOrDefaultAsync(t => t.Id == tierId && t.EventId == id);
        if (tier is not null)
        {
            tier.Name = tierName;
            tier.Description = tierDescription;
            tier.Price = tierPrice;
            tier.TotalQuantity = tierQuantity;
            tier.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteTierAsync(Guid id, Guid tierId)
    {
        var tier = await _db.TicketTiers.FirstOrDefaultAsync(t => t.Id == tierId && t.EventId == id);
        if (tier is not null)
        {
            tier.IsDeleted = true;
            tier.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id });
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

    private async Task LoadTiersAsync(Guid eventId)
    {
        Tiers = await _db.TicketTiers
            .AsNoTracking()
            .Where(t => t.EventId == eventId)
            .OrderBy(t => t.Price)
            .Select(t => new TierRow
            {
                Id = t.Id, Name = t.Name, Description = t.Description,
                Price = t.Price, TotalQuantity = t.TotalQuantity,
                SoldQuantity = t.SoldQuantity, AvailableQuantity = t.AvailableQuantity
            })
            .ToListAsync();
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
