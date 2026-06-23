using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GridTickets.Infrastructure.Data.Seed;

public class DbSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<DbSeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in Roles.All)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole(roleName)
                {
                    Description = $"{roleName} role"
                };
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                    _logger.LogInformation("Role '{RoleName}' created.", roleName);
                else
                    _logger.LogError("Failed to create role '{RoleName}': {Errors}", roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@gridtickets.com";
        var admin = await _userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                EmailConfirmed = true,
                Status = UserStatus.Active
            };

            var result = await _userManager.CreateAsync(admin, "Admin@123456");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, Roles.Admin);
                _logger.LogInformation("Admin user seeded: {Email}", adminEmail);
            }
            else
            {
                _logger.LogError("Failed to seed admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
