using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages;

public class DashboardUserRow
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class IndexModel : PageModel
{
    private readonly GridTicketsDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public int TotalUsers { get; private set; }
    public IList<DashboardUserRow> RecentUsers { get; private set; } = new List<DashboardUserRow>();

    public IndexModel(GridTicketsDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task OnGetAsync()
    {
        TotalUsers = await _db.Users.CountAsync();

        var recent = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(10)
            .ToListAsync();

        foreach (var user in recent)
        {
            var roles = await _userManager.GetRolesAsync(user);
            RecentUsers.Add(new DashboardUserRow
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            });
        }
    }
}
