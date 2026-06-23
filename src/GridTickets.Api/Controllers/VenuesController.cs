using GridTickets.Application.DTOs.Common;
using GridTickets.Application.DTOs.Venues;
using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly GridTicketsDbContext _db;

    public VenuesController(GridTicketsDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] PagedQuery query)
    {
        var q = _db.Venues.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            q = q.Where(v => v.Name.Contains(query.SearchTerm) || v.City.Contains(query.SearchTerm));

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(v => v.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(v => new VenueDto
            {
                Id = v.Id, Name = v.Name, Address = v.Address,
                City = v.City, State = v.State, Capacity = v.Capacity,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<VenueDto>
        {
            Items = items, TotalCount = total,
            PageNumber = query.PageNumber, PageSize = query.PageSize
        });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var v = await _db.Venues.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (v is null) return NotFound();
        return Ok(new VenueDto
        {
            Id = v.Id, Name = v.Name, Address = v.Address,
            City = v.City, State = v.State, Capacity = v.Capacity,
            CreatedAt = v.CreatedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateVenueRequest req)
    {
        var venue = new Venue
        {
            Name = req.Name, Address = req.Address,
            City = req.City, State = req.State, Capacity = req.Capacity
        };
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = venue.Id }, new VenueDto
        {
            Id = venue.Id, Name = venue.Name, Address = venue.Address,
            City = venue.City, State = venue.State, Capacity = venue.Capacity,
            CreatedAt = venue.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVenueRequest req)
    {
        var venue = await _db.Venues.FindAsync(id);
        if (venue is null) return NotFound();

        venue.Name = req.Name;
        venue.Address = req.Address;
        venue.City = req.City;
        venue.State = req.State;
        venue.Capacity = req.Capacity;
        venue.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var venue = await _db.Venues.FindAsync(id);
        if (venue is null) return NotFound();

        venue.IsDeleted = true;
        venue.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
