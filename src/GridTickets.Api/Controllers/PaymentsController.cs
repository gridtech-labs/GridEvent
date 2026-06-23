using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GridTickets.Domain.Enums;
using GridTickets.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridTickets.Api.Controllers;

public class RazorpaySettings
{
    public bool IsLive { get; set; }
    public string TestKeyId { get; set; } = string.Empty;
    public string TestKeySecret { get; set; } = string.Empty;
    public string LiveKeyId { get; set; } = string.Empty;
    public string LiveKeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    public string ActiveKeyId => IsLive ? LiveKeyId : TestKeyId;
    public string ActiveKeySecret => IsLive ? LiveKeySecret : TestKeySecret;
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly GridTicketsDbContext _db;
    private readonly RazorpaySettings _rzp;
    private readonly IHttpClientFactory _httpFactory;
    private const string RazorpayOrdersUrl = "https://api.razorpay.com/v1/orders";

    public PaymentsController(
        GridTicketsDbContext db,
        IOptions<RazorpaySettings> rzpOptions,
        IHttpClientFactory httpFactory)
    {
        _db = db;
        _rzp = rzpOptions.Value;
        _httpFactory = httpFactory;
    }

    // ── POST /api/payments/initiate ───────────────────────────────────────────

    /// <summary>Create a Razorpay order and return checkout params.</summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.UserId == userId);

        if (order is null) return NotFound(new { message = "Order not found." });
        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { message = "Order is no longer pending." });
        if (order.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "Order has expired." });

        var amountPaise = (long)(order.GrandTotal * 100); // ₹ → paise
        var rzpOrderId = await CreateRazorpayOrderAsync(amountPaise, order.Id.ToString());

        if (rzpOrderId is null)
            return StatusCode(502, new { message = "Failed to create payment order. Please try again." });

        order.RazorpayOrderId = rzpOrderId;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            keyId = _rzp.ActiveKeyId,
            razorpayOrderId = rzpOrderId,
            amount = order.GrandTotal,
            currency = "INR"
        });
    }

    // ── POST /api/payments/verify ─────────────────────────────────────────────

    /// <summary>Verify Razorpay payment signature and confirm the order.</summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyPaymentRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == req.OrderId && o.UserId == userId);

        if (order is null) return NotFound(new { message = "Order not found." });
        if (order.Status == OrderStatus.Confirmed)
            return Ok(new { status = "Confirmed" }); // idempotent

        // HMAC-SHA256: razorpayOrderId|razorpayPaymentId
        var expectedSig = ComputeHmac(
            $"{req.RazorpayOrderId}|{req.RazorpayPaymentId}",
            _rzp.ActiveKeySecret);

        if (!string.Equals(expectedSig, req.RazorpaySignature, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Payment verification failed. Invalid signature." });

        order.Status = OrderStatus.Confirmed;
        order.RazorpayPaymentId = req.RazorpayPaymentId;
        order.BookingReference = GenerateBookingReference();
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { status = "Confirmed", orderId = order.Id });
    }

    // ── POST /api/webhooks/razorpay ───────────────────────────────────────────

    /// <summary>Razorpay webhook — payment.captured event confirms the order server-side.</summary>
    [HttpPost("/api/webhooks/razorpay")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        if (!string.IsNullOrEmpty(_rzp.WebhookSecret) && !string.IsNullOrEmpty(signature))
        {
            var expected = ComputeHmac(body, _rzp.WebhookSecret);
            if (!string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase))
                return Unauthorized();
        }

        using var doc = JsonDocument.Parse(body);
        var eventType = doc.RootElement.GetProperty("event").GetString();

        if (eventType == "payment.captured")
        {
            var paymentEntity = doc.RootElement
                .GetProperty("payload").GetProperty("payment").GetProperty("entity");

            var rzpOrderId = paymentEntity.GetProperty("order_id").GetString();
            var rzpPaymentId = paymentEntity.TryGetProperty("id", out var pidProp)
                ? pidProp.GetString()
                : null;

            if (!string.IsNullOrEmpty(rzpOrderId))
            {
                var order = await _db.Orders.FirstOrDefaultAsync(o => o.RazorpayOrderId == rzpOrderId);
                if (order is { Status: OrderStatus.Pending })
                {
                    order.Status = OrderStatus.Confirmed;
                    order.RazorpayPaymentId ??= rzpPaymentId;
                    order.BookingReference ??= GenerateBookingReference();
                    order.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
        }

        return Ok();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string?> CreateRazorpayOrderAsync(long amountPaise, string receipt)
    {
        var client = _httpFactory.CreateClient();
        var creds = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_rzp.ActiveKeyId}:{_rzp.ActiveKeySecret}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", creds);

        var payload = JsonSerializer.Serialize(new
        {
            amount = amountPaise,
            currency = "INR",
            receipt = receipt[..Math.Min(receipt.Length, 40)]
        });

        var response = await client.PostAsync(
            RazorpayOrdersUrl,
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString();
    }

    private static string GenerateBookingReference()
        => "GT" + Guid.NewGuid().ToString("N")[..8].ToUpper();

    private static string ComputeHmac(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message))).ToLower();
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record InitiatePaymentRequest(Guid OrderId);

public record VerifyPaymentRequest(
    Guid OrderId,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature);
