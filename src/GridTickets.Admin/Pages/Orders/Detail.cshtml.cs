using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Orders;

public class OrderDetailRow
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string EventVenue { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal BookingFee { get; set; }
    public decimal GrandTotal { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<OrderItemRow> Items { get; set; } = new();
}

public class OrderItemRow
{
    public string TierName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderDetailModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    public OrderDetailRow? Order { get; private set; }

    public string StatusBadgeClass => Order?.Status switch
    {
        "Confirmed" => "bg-success",
        "Pending" => "bg-warning text-dark",
        "Cancelled" or "Expired" => "bg-secondary",
        "Failed" => "bg-danger",
        _ => "bg-light text-dark"
    };

    public OrderDetailModel(GridTicketsDbContext db) => _db = db;

    public async Task OnGetAsync(Guid id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Include(o => o.Event).ThenInclude(e => e.Venue)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return;

        Order = new OrderDetailRow
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            EventTitle = order.Event?.Title ?? "—",
            EventVenue = order.Event?.Venue != null
                ? $"{order.Event.Venue.Name}, {order.Event.Venue.City}"
                : "—",
            EventStartDate = order.Event?.StartDate ?? default,
            Status = order.Status.ToString(),
            SubTotal = order.SubTotal,
            BookingFee = order.BookingFee,
            GrandTotal = order.GrandTotal,
            RazorpayOrderId = order.RazorpayOrderId,
            RazorpayPaymentId = order.RazorpayPaymentId,
            CreatedAt = order.CreatedAt,
            ExpiresAt = order.ExpiresAt,
            Items = order.Items.Select(i => new OrderItemRow
            {
                TierName = i.TierName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.UnitPrice * i.Quantity
            }).ToList()
        };
    }
}
