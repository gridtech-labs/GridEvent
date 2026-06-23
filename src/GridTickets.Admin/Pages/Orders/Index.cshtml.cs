using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GridTickets.Admin.Pages.Orders;

public class OrderRow
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class OrdersIndexModel : PageModel
{
    private readonly GridTicketsDbContext _db;

    public IList<OrderRow> Orders { get; private set; } = new List<OrderRow>();
    public int TotalCount { get; private set; }
    public int TotalPages { get; private set; }
    public new int Page { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public string? Search { get; private set; }
    public string? StatusFilter { get; private set; }

    public OrdersIndexModel(GridTicketsDbContext db) => _db = db;

    public async Task OnGetAsync(string? search, string? status, int page = 1)
    {
        Page = page < 1 ? 1 : page;
        Search = search;
        StatusFilter = status;

        var query = _db.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Include(o => o.Event)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            query = query.Where(o => o.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(o =>
                o.CustomerEmail.ToLower().Contains(term) ||
                o.CustomerName.ToLower().Contains(term) ||
                (o.RazorpayOrderId != null && o.RazorpayOrderId.Contains(term)));
        }

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

        Orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .Select(o => new OrderRow
            {
                Id = o.Id,
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                EventTitle = o.Event.Title,
                ItemCount = o.Items.Sum(i => i.Quantity),
                GrandTotal = o.GrandTotal,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt,
                ExpiresAt = o.ExpiresAt
            })
            .ToListAsync();
    }
}
