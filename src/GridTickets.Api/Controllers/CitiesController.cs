using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Api.Controllers;

public record CityDto(Guid Id, string Name, string State, string? ImageUrl, int SortOrder);

[ApiController]
[Route("api/[controller]")]
public class CitiesController : ControllerBase
{
    private readonly GridTicketsDbContext _db;
    public CitiesController(GridTicketsDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var cities = await _db.Cities
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CityDto(c.Id, c.Name, c.State, c.ImageUrl, c.SortOrder))
            .ToListAsync();

        return Ok(cities);
    }
}
