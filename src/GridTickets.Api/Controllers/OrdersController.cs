using GridTickets.Application.DTOs.Common;
using GridTickets.Application.DTOs.Orders;
using GridTickets.Domain.Entities;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GridTickets.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly GridTicketsDbContext _db;

    public OrdersController(GridTicketsDbContext db) => _db = db;

    /// <summary>Create a new pending order, locking the selected tickets for 10 minutes.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (!req.Items.Any())
            return BadRequest(new { message = "At least one ticket item is required." });

        // Validate all tiers belong to the event and have enough availability
        var tierIds = req.Items.Select(i => i.TicketTierId).ToList();
        var tiers = await _db.TicketTiers
            .AsNoTracking()
            .Where(t => tierIds.Contains(t.Id) && t.EventId == req.EventId)
            .ToListAsync();

        if (tiers.Count != req.Items.Count)
            return BadRequest(new { message = "One or more ticket tiers are invalid for this event." });

        foreach (var item in req.Items)
        {
            var tier = tiers.First(t => t.Id == item.TicketTierId);
            if (item.Quantity < 1 || item.Quantity > 10)
                return BadRequest(new { message = $"Quantity for '{tier.Name}' must be between 1 and 10." });
            if (tier.AvailableQuantity < item.Quantity)
                return BadRequest(new { message = $"Not enough tickets available for '{tier.Name}'." });
        }

        // Deduct quantities atomically using a conditional UPDATE so that two
        // concurrent requests cannot both oversell the last ticket.
        // Each UPDATE only succeeds if SoldQuantity + requested <= TotalQuantity.
        var orderItems = new List<OrderItem>();
        decimal subTotal = 0;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var item in req.Items)
        {
            var tier = tiers.First(t => t.Id == item.TicketTierId);

            // Atomic conditional decrement: only applies the update when enough
            // stock is still available at the moment of execution.
            var affected = await _db.TicketTiers
                .Where(t => t.Id == item.TicketTierId && (t.TotalQuantity - t.SoldQuantity) >= item.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.SoldQuantity, t => t.SoldQuantity + item.Quantity));

            if (affected == 0)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = $"Not enough tickets available for '{tier.Name}'. Please refresh and try again." });
            }

            orderItems.Add(new OrderItem
            {
                TicketTierId = tier.Id,
                TierName = tier.Name,
                Quantity = item.Quantity,
                UnitPrice = tier.Price
            });
            subTotal += tier.Price * item.Quantity;
        }

        var bookingFee = Math.Round(subTotal * 0.035m, 2); // 3.5% booking fee
        var order = new Order
        {
            UserId = userId,
            EventId = req.EventId,
            Status = OrderStatus.Pending,
            SubTotal = subTotal,
            BookingFee = bookingFee,
            GrandTotal = subTotal + bookingFee,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CustomerName = req.CustomerName,
            CustomerEmail = req.CustomerEmail,
            CustomerPhone = req.CustomerPhone,
            Items = orderItems
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, await MapOrderAsync(order.Id));
    }

    /// <summary>Get a specific order (owner or admin only).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole(Roles.Admin);

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Event).ThenInclude(e => e.Venue)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();
        if (!isAdmin && order.UserId != userId) return Forbid();

        return Ok(MapOrder(order));
    }

    /// <summary>Get current user's orders.</summary>
    [HttpGet("my")]
    public async Task<IActionResult> MyOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Event).ThenInclude(e => e.Venue)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync();
        var orders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<OrderDto>
        {
            Items = orders.Select(MapOrder).ToList(),
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>Admin: list all orders with optional filters.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Event).ThenInclude(e => e.Venue)
            .IgnoreQueryFilters()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            query = query.Where(o => o.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o =>
                o.CustomerEmail.Contains(search) ||
                o.CustomerName.Contains(search) ||
                o.RazorpayOrderId!.Contains(search));

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<OrderDto>
        {
            Items = orders.Select(MapOrder).ToList(),
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>Cancel a pending order and restore ticket availability.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole(Roles.Admin);

        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();
        if (!isAdmin && order.UserId != userId) return Forbid();
        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { message = "Only pending orders can be cancelled." });

        await RestoreTicketQuantitiesAsync(order);
        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task RestoreTicketQuantitiesAsync(Order order)
    {
        foreach (var item in order.Items)
        {
            await _db.TicketTiers
                .Where(t => t.Id == item.TicketTierId)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    t => t.SoldQuantity,
                    t => t.SoldQuantity >= item.Quantity ? t.SoldQuantity - item.Quantity : 0));
        }
    }

    private async Task<OrderDto> MapOrderAsync(Guid orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Event).ThenInclude(e => e.Venue)
            .FirstAsync(o => o.Id == orderId);
        return MapOrder(order);
    }

    private static OrderDto MapOrder(Order o) => new()
    {
        Id = o.Id,
        EventId = o.EventId,
        EventTitle = o.Event?.Title ?? string.Empty,
        EventVenue = o.Event?.Venue != null ? $"{o.Event.Venue.Name}, {o.Event.Venue.City}" : string.Empty,
        EventStartDate = o.Event?.StartDate ?? default,
        Status = o.Status.ToString(),
        SubTotal = o.SubTotal,
        BookingFee = o.BookingFee,
        GrandTotal = o.GrandTotal,
        RazorpayOrderId = o.RazorpayOrderId,
        BookingReference = o.BookingReference,
        ExpiresAt = o.ExpiresAt,
        CreatedAt = o.CreatedAt,
        CustomerName = o.CustomerName,
        CustomerEmail = o.CustomerEmail,
        CustomerPhone = o.CustomerPhone,
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            TicketTierId = i.TicketTierId,
            TierName = i.TierName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.UnitPrice * i.Quantity
        }).ToList()
    };
}
