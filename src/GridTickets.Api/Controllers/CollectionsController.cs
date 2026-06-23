using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollectionsController : ControllerBase
{
    private readonly GridTicketsDbContext _db;

    public CollectionsController(GridTicketsDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var collections = await _db.Collections
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CollectionDto(c.Id, c.Name, c.Description, c.SortOrder))
            .ToListAsync();

        return Ok(collections);
    }
}

public record CollectionDto(Guid Id, string Name, string? Description, int SortOrder);
