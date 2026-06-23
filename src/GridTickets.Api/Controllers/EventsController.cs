using GridTickets.Application.DTOs.Common;
using GridTickets.Application.DTOs.Events;
using GridTickets.Application.DTOs.TicketTiers;
using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly GridTicketsDbContext _db;

    public EventsController(GridTicketsDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQuery query,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? city)
    {
        var q = _db.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .Include(e => e.Category)
            .Include(e => e.Collection)
            .Include(e => e.TicketTiers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            q = q.Where(e => e.Title.Contains(query.SearchTerm));

        if (categoryId.HasValue)
            q = q.Where(e => e.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EventStatus>(status, true, out var parsedStatus))
            q = q.Where(e => e.Status == parsedStatus);

        if (from.HasValue) q = q.Where(e => e.StartDate >= from.Value);
        if (to.HasValue) q = q.Where(e => e.StartDate <= to.Value);

        if (!string.IsNullOrWhiteSpace(city))
            q = q.Where(e => e.Venue != null && e.Venue.City == city);

        var total = await q.CountAsync();
        var events = await q
            .OrderBy(e => e.StartDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = events.Select(e => new EventSummaryDto
        {
            Id = e.Id, Title = e.Title, BannerImageUrl = e.BannerImageUrl,
            StartDate = e.StartDate, EndDate = e.EndDate,
            Status = e.Status.ToString(),
            VenueName = e.Venue?.Name ?? string.Empty,
            VenueCity = e.Venue?.City ?? string.Empty,
            CategoryName = e.Category?.Name ?? string.Empty,
            CollectionId = e.CollectionId,
            CollectionName = e.Collection?.Name,
            MinPrice = e.TicketTiers.Any() ? e.TicketTiers.Min(t => t.Price) : 0,
            TicketTiers = e.TicketTiers.Select(t => new TicketTierDto
            {
                Id = t.Id, Name = t.Name, Description = t.Description,
                Price = t.Price, TotalQuantity = t.TotalQuantity,
                SoldQuantity = t.SoldQuantity, AvailableQuantity = t.AvailableQuantity,
                EventId = t.EventId
            }).ToList()
        }).ToList();

        return Ok(new PagedResult<EventSummaryDto>
        {
            Items = items, TotalCount = total,
            PageNumber = query.PageNumber, PageSize = query.PageSize
        });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var e = await _db.Events
            .AsNoTracking()
            .Include(x => x.Venue)
            .Include(x => x.Category)
            .Include(x => x.Collection)
            .Include(x => x.TicketTiers)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e is null) return NotFound();

        return Ok(MapToDto(e));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest req)
    {
        if (!Enum.TryParse<EventStatus>(req.Status, true, out var status))
            return BadRequest(new { message = "Invalid status value." });

        if (!await _db.Venues.AnyAsync(v => v.Id == req.VenueId))
            return BadRequest(new { message = "Venue not found." });

        if (!await _db.Categories.AnyAsync(c => c.Id == req.CategoryId))
            return BadRequest(new { message = "Category not found." });

        var ev = new Event
        {
            Title = req.Title, Description = req.Description,
            StartDate = req.StartDate, EndDate = req.EndDate,
            BannerImageUrl = req.BannerImageUrl, Status = status,
            VenueId = req.VenueId, CategoryId = req.CategoryId
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var created = await _db.Events
            .Include(x => x.Venue).Include(x => x.Category)
            .Include(x => x.Collection).Include(x => x.TicketTiers)
            .FirstAsync(x => x.Id == ev.Id);

        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, MapToDto(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest req)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();

        if (!Enum.TryParse<EventStatus>(req.Status, true, out var status))
            return BadRequest(new { message = "Invalid status value." });

        if (!await _db.Venues.AnyAsync(v => v.Id == req.VenueId))
            return BadRequest(new { message = "Venue not found." });

        if (!await _db.Categories.AnyAsync(c => c.Id == req.CategoryId))
            return BadRequest(new { message = "Category not found." });

        ev.Title = req.Title;
        ev.Description = req.Description;
        ev.StartDate = req.StartDate;
        ev.EndDate = req.EndDate;
        ev.BannerImageUrl = req.BannerImageUrl;
        ev.Status = status;
        ev.VenueId = req.VenueId;
        ev.CategoryId = req.CategoryId;
        ev.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();

        ev.IsDeleted = true;
        ev.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static EventDto MapToDto(Event e) => new()
    {
        Id = e.Id, Title = e.Title, Description = e.Description,
        BannerImageUrl = e.BannerImageUrl,
        StartDate = e.StartDate, EndDate = e.EndDate,
        Status = e.Status.ToString(),
        VenueId = e.VenueId, VenueName = e.Venue.Name,
        VenueCity = e.Venue.City, VenueAddress = e.Venue.Address,
        VenueState = e.Venue.State, VenueCapacity = e.Venue.Capacity,
        CategoryId = e.CategoryId, CategoryName = e.Category.Name,
        CategorySlug = e.Category.Slug,
        CollectionId = e.CollectionId,
        CollectionName = e.Collection?.Name,
        CreatedAt = e.CreatedAt,
        MinPrice = e.TicketTiers.Any() ? e.TicketTiers.Min(t => t.Price) : 0,
        TicketTiers = e.TicketTiers.Select(t => new TicketTierDto
        {
            Id = t.Id, Name = t.Name, Description = t.Description,
            Price = t.Price, TotalQuantity = t.TotalQuantity,
            SoldQuantity = t.SoldQuantity, AvailableQuantity = t.AvailableQuantity,
            EventId = t.EventId
        }).ToList()
    };
}
