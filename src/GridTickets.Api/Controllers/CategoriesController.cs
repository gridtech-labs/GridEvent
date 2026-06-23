using GridTickets.Application.DTOs.Categories;
using GridTickets.Application.DTOs.Common;
using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly GridTicketsDbContext _db;

    public CategoriesController(GridTicketsDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] PagedQuery query)
    {
        var q = _db.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            q = q.Where(c => c.Name.Contains(query.SearchTerm) || c.Slug.Contains(query.SearchTerm));

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(c => c.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CategoryDto
            {
                Id = c.Id, Name = c.Name, Slug = c.Slug,
                Description = c.Description, CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedResult<CategoryDto>
        {
            Items = items, TotalCount = total,
            PageNumber = query.PageNumber, PageSize = query.PageSize
        });
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var c = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        return Ok(new CategoryDto
        {
            Id = c.Id, Name = c.Name, Slug = c.Slug,
            Description = c.Description, CreatedAt = c.CreatedAt
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        if (await _db.Categories.AnyAsync(c => c.Slug == req.Slug))
            return Conflict(new { message = "A category with this slug already exists." });

        var category = new Category
        {
            Name = req.Name, Slug = req.Slug.ToLowerInvariant(),
            Description = req.Description
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, new CategoryDto
        {
            Id = category.Id, Name = category.Name, Slug = category.Slug,
            Description = category.Description, CreatedAt = category.CreatedAt
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest req)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        if (await _db.Categories.AnyAsync(c => c.Slug == req.Slug && c.Id != id))
            return Conflict(new { message = "A category with this slug already exists." });

        category.Name = req.Name;
        category.Slug = req.Slug.ToLowerInvariant();
        category.Description = req.Description;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
