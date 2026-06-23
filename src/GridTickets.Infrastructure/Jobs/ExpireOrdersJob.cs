using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GridTickets.Infrastructure.Jobs;

public class ExpireOrdersJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpireOrdersJob> _logger;

    public ExpireOrdersJob(IServiceScopeFactory scopeFactory, ILogger<ExpireOrdersJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GridTicketsDbContext>();

        var expiredOrders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredOrders.Count == 0) return;

        var tierIds = expiredOrders.SelectMany(o => o.Items.Select(i => i.TicketTierId)).Distinct().ToList();
        var tiers = await db.TicketTiers.Where(t => tierIds.Contains(t.Id)).ToListAsync();

        foreach (var order in expiredOrders)
        {
            foreach (var item in order.Items)
            {
                var tier = tiers.FirstOrDefault(t => t.Id == item.TicketTierId);
                if (tier is not null)
                    tier.SoldQuantity = Math.Max(0, tier.SoldQuantity - item.Quantity);
            }
            order.Status = OrderStatus.Expired;
            order.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Expired {Count} pending orders and restored ticket availability.", expiredOrders.Count);
    }
}
