using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GridTickets.Infrastructure.Data;

public class GridTicketsDbContextFactory : IDesignTimeDbContextFactory<GridTicketsDbContext>
{
    public GridTicketsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../GridTickets.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<GridTicketsDbContext>();
        optionsBuilder.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.MigrationsAssembly(typeof(GridTicketsDbContext).Assembly.FullName));

        return new GridTicketsDbContext(optionsBuilder.Options);
    }
}
