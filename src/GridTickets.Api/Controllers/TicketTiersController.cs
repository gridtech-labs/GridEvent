using GridTickets.Application.DTOs.TicketTiers;
using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/tiers")]
public class TicketTiersController : ControllerBase
{
    private readonly GridTicketsDbContext _db;

    public TicketTiersController(GridTicketsDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(Guid eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId))
            return NotFound(new { message = "Event not found." });

        var tiers = await _db.TicketTiers
            .AsNoTracking()
            .Where(t => t.EventId == eventId)
            .OrderBy(t => t.Price)
            .Select(t => new TicketTierDto
            {
                Id = t.Id, Name = t.Name, Description = t.Description,
                Price = t.Price, TotalQuantity = t.TotalQuantity,
                SoldQuantity = t.SoldQuantity, AvailableQuantity = t.AvailableQuantity,
                EventId = t.EventId
            })
            .ToListAsync();

        return Ok(tiers);
    }

    [HttpGet("{tierId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid eventId, Guid tierId)
    {
        var tier = await _db.TicketTiers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tierId && t.EventId == eventId);

        if (tier is null) return NotFound();

        return Ok(new TicketTierDto
        {
            Id = tier.Id, Name = tier.Name, Description = tier.Description,
            Price = tier.Price, TotalQuantity = tier.TotalQuantity,
            SoldQuantity = tier.SoldQuantity, AvailableQuantity = tier.AvailableQuantity,
            EventId = tier.EventId
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(Guid eventId, [FromBody] CreateTicketTierRequest req)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId))
            return NotFound(new { message = "Event not found." });

        var tier = new TicketTier
        {
            Name = req.Name, Description = req.Description,
            Price = req.Price, TotalQuantity = req.TotalQuantity,
            EventId = eventId
        };
        _db.TicketTiers.Add(tier);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { eventId, tierId = tier.Id }, new TicketTierDto
        {
            Id = tier.Id, Name = tier.Name, Description = tier.Description,
            Price = tier.Price, TotalQuantity = tier.TotalQuantity,
            SoldQuantity = tier.SoldQuantity, AvailableQuantity = tier.AvailableQuantity,
            EventId = tier.EventId
        });
    }

    [HttpPut("{tierId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid eventId, Guid tierId, [FromBody] UpdateTicketTierRequest req)
    {
        var tier = await _db.TicketTiers.FirstOrDefaultAsync(t => t.Id == tierId && t.EventId == eventId);
        if (tier is null) return NotFound();

        tier.Name = req.Name;
        tier.Description = req.Description;
        tier.Price = req.Price;
        tier.TotalQuantity = req.TotalQuantity;
        tier.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{tierId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid eventId, Guid tierId)
    {
        var tier = await _db.TicketTiers.FirstOrDefaultAsync(t => t.Id == tierId && t.EventId == eventId);
        if (tier is null) return NotFound();

        tier.IsDeleted = true;
        tier.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
