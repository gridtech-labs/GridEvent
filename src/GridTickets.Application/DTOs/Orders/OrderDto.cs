namespace GridTickets.Application.DTOs.Orders;

public class CreateOrderRequest
{
    public Guid EventId { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
}

public class OrderItemRequest
{
    public Guid TicketTierId { get; set; }
    public int Quantity { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid TicketTierId { get; set; }
    public string TierName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventVenue { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal BookingFee { get; set; }
    public decimal GrandTotal { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? BookingReference { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
}

public class InitiatePaymentRequest
{
    public Guid OrderId { get; set; }
}

public class InitiatePaymentResponse
{
    public string RazorpayOrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string KeyId { get; set; } = string.Empty;
}

public class VerifyPaymentRequest
{
    public Guid OrderId { get; set; }
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}
