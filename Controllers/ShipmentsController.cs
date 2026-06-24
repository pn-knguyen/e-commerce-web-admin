using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Integrations.GiaoHangNhanh;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.Shipping;
using e_commerce_web_admin.ViewModels.Shipments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace e_commerce_web_admin.Controllers;

public sealed class ShipmentsController : Controller
{
    private readonly IShipmentAdminService _shipmentService;
    private readonly GiaoHangNhanhOptions _ghnOptions;

    public ShipmentsController(
        IShipmentAdminService shipmentService,
        IOptions<GiaoHangNhanhOptions> ghnOptions)
    {
        _shipmentService = shipmentService;
        _ghnOptions = ghnOptions.Value;
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Orders", Permissions.Approve)]
    public async Task<IActionResult> CreateQuote(
        long orderId,
        ShipmentQuoteCreateViewModel form,
        CancellationToken ct)
    {
        if (orderId != form.OrderId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin kiện hàng.";
            return RedirectToOrder(orderId);
        }

        var result = await _shipmentService.CreateQuoteAsync(orderId, form, GetCurrentStaffId(), ct);
        return HandleActionResult(orderId, result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Orders", Permissions.Approve)]
    public async Task<IActionResult> BookShipment(long orderId, long shipmentId, CancellationToken ct)
    {
        var result = await _shipmentService.BookShipmentAsync(orderId, shipmentId, ct);
        return HandleActionResult(orderId, result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Orders", Permissions.Approve)]
    public async Task<IActionResult> CancelShipment(long orderId, long shipmentId, CancellationToken ct)
    {
        var result = await _shipmentService.CancelShipmentAsync(orderId, shipmentId, ct);
        return HandleActionResult(orderId, result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Orders", Permissions.Approve)]
    public async Task<IActionResult> SyncShipmentStatus(long orderId, long shipmentId, CancellationToken ct)
    {
        var result = await _shipmentService.SyncShipmentStatusAsync(orderId, shipmentId, ct);
        return HandleActionResult(orderId, result);
    }

    [HttpPost("/api/giao-hang-nhanh/webhook")]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(1_048_576)]
    public async Task<IActionResult> GiaoHangNhanhWebhook(CancellationToken ct)
    {
        if (!_ghnOptions.EnableWebhookProcessing)
        {
            return Accepted(new { message = "Webhook GHN đang tắt. Hệ thống đang đồng bộ trạng thái bằng API nền." });
        }

        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync(ct);
        if (!IsWebhookRequestAuthorized(rawPayload))
        {
            return Unauthorized(new { message = "Webhook GHN không hợp lệ." });
        }

        var result = await _shipmentService.HandleProviderWebhookAsync(rawPayload, ct);
        return result.Found && result.Succeeded ? Ok() : BadRequest(new { message = result.Message });
    }

    private IActionResult HandleActionResult(long orderId, ShipmentActionResult result)
    {
        if (!result.Found)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToOrder(orderId);
    }

    private RedirectToActionResult RedirectToOrder(long orderId) =>
        RedirectToAction("Details", "Orders", new { id = orderId });

    private bool IsWebhookRequestAuthorized(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(_ghnOptions.WebhookSecret))
        {
            return false;
        }

        return _ghnOptions.EnableWebhookSignatureValidation
            ? HasValidWebhookSignature(rawPayload, _ghnOptions.WebhookSecret)
            : HasValidWebhookSharedSecret(_ghnOptions.WebhookSecret);
    }

    private bool HasValidWebhookSharedSecret(string secret)
    {
        var providedSecret =
            Request.Headers["X-Webhook-Secret"].FirstOrDefault() ??
            Request.Headers["X-GHN-Webhook-Secret"].FirstOrDefault() ??
            Request.Query["secret"].FirstOrDefault();

        return FixedTimeEquals(providedSecret, secret);
    }

    private bool HasValidWebhookSignature(string rawPayload, string secret)
    {
        var providedSignature =
            Request.Headers["X-GHN-Signature"].FirstOrDefault() ??
            Request.Headers["X-Hub-Signature-256"].FirstOrDefault() ??
            Request.Headers["X-Signature"].FirstOrDefault() ??
            Request.Headers["X-Webhook-Signature"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedSignature))
        {
            return false;
        }

        var payloadBytes = Encoding.UTF8.GetBytes(rawPayload);
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(secretBytes, payloadBytes);
        var hexSignature = Convert.ToHexString(hash).ToLowerInvariant();
        var base64Signature = Convert.ToBase64String(hash);
        var normalizedSignature = providedSignature.Trim();
        if (normalizedSignature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            normalizedSignature = normalizedSignature["sha256=".Length..];
        }

        return FixedTimeEquals(normalizedSignature.ToLowerInvariant(), hexSignature) ||
            FixedTimeEquals(normalizedSignature, base64Signature);
    }

    private static bool FixedTimeEquals(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left.Trim());
        var rightBytes = Encoding.UTF8.GetBytes(right.Trim());
        return leftBytes.Length == rightBytes.Length &&
            CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private long? GetCurrentStaffId()
    {
        var value = User.FindFirst(AppClaimTypes.UserId)?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(value, out var id) ? id : null;
    }
}
