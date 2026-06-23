using GridTickets.Domain.Entities;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Users;

public class UserRow
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UsersIndexModel : PageModel
{
    private readonly GridTicketsDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IList<UserRow> Users { get; private set; } = new List<UserRow>();
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public new int Page { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public string? Search { get; private set; }
    public string? RoleFilter { get; private set; }

    public UsersIndexModel(GridTicketsDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task OnGetAsync(string? search, string? role, int page = 1)
    {
        Page = page < 1 ? 1 : page;
        Search = search;
        RoleFilter = role;

        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrEmpty(RoleFilter) && !roles.Contains(RoleFilter))
                continue;

            Users.Add(new UserRow
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Roles = roles,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            });
        }
    }
}
