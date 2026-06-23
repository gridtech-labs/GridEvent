using GridTickets.Application.DTOs.Common;
using GridTickets.Application.DTOs.Users;
using GridTickets.Application.Interfaces;
using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly GridTicketsDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(
        ICurrentUserService currentUser,
        GridTicketsDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _currentUser = currentUser;
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("me")]
    public IActionResult GetProfile()
    {
        return Ok(new
        {
            _currentUser.UserId,
            _currentUser.Email,
            _currentUser.Roles
        });
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetUsers([FromQuery] PagedQuery query, [FromQuery] string? status)
    {
        var q = _db.Users.AsNoTracking().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            q = q.Where(u => u.Email!.Contains(query.SearchTerm) ||
                              u.FirstName.Contains(query.SearchTerm) ||
                              u.LastName.Contains(query.SearchTerm));

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, true, out var parsedStatus))
            q = q.Where(u => u.Status == parsedStatus);

        var total = await q.CountAsync();
        var users = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = new List<UserDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            items.Add(new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                Status = u.Status.ToString(),
                IsDeleted = u.IsDeleted,
                CreatedAt = u.CreatedAt,
                Roles = roles
            });
        }

        return Ok(new PagedResult<UserDto>
        {
            Items = items, TotalCount = total,
            PageNumber = query.PageNumber, PageSize = query.PageSize
        });
    }
}
