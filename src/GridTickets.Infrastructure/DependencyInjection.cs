using GridTickets.Application.Interfaces;
using GridTickets.Domain.Entities;
using GridTickets.Infrastructure.Data;
using GridTickets.Infrastructure.Data.Seed;
using GridTickets.Infrastructure.Jobs;
using GridTickets.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace GridTickets.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Resolves the Npgsql connection string from either the standard
    /// ConnectionStrings:DefaultConnection config key or the DATABASE_URL
    /// environment variable (Railway / Heroku postgresql:// URL format).
    /// </summary>
    public static string ResolveConnectionString(IConfiguration configuration)
    {
        // Check all Railway-style env vars in priority order
        var raw = Environment.GetEnvironmentVariable("DATABASE_URL")
                  ?? Environment.GetEnvironmentVariable("DATABASE_PUBLIC_URL")
                  ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(raw))
            throw new InvalidOperationException(
                "No database connection string configured. " +
                "Set DATABASE_URL, DATABASE_PUBLIC_URL, or ConnectionStrings__DefaultConnection.");

        return ConvertToNpgsql(raw);
    }

    private static string ConvertToNpgsql(string cs)
    {
        if (!cs.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
            !cs.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            return cs;

        var uri = new Uri(cs);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var port = uri.Port < 0 ? 5432 : uri.Port;
        var database = uri.AbsolutePath.TrimStart('/');

        Console.WriteLine($"[DB] Resolved host={uri.Host} port={port} db={database}");

        return $"Host={uri.Host};Port={port};Database={database};" +
               $"Username={username};Password={password};" +
               $"SSL Mode=Require;Trust Server Certificate=true";
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);

        // PostgreSQL + EF Core
        services.AddDbContext<GridTicketsDbContext>(options =>
            options
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(GridTicketsDbContext).Assembly.FullName))
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<GridTicketsDbContext>()
        .AddDefaultTokenProviders();

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            var redisOptions = ConfigurationOptions.Parse(redisConnection);
            redisOptions.AbortOnConnectFail = false;
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisOptions));
        }

        // Register job class for DI resolution by Hangfire hosts
        services.AddSingleton<ExpireOrdersJob>();

        // OTP + Notification
        services.AddMemoryCache();
        services.AddSingleton<IOtpService>(sp =>
            new OtpService(
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                sp.GetService<IConnectionMultiplexer>()));
        services.AddScoped<INotificationService, NotificationService>();

        // Application services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<DbSeeder>();
        services.AddHttpContextAccessor();

        return services;
    }
}
