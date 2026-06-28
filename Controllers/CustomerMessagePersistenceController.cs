using System.Security.Claims;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Services.CustomerMessages;
using e_commerce_web_admin.ViewModels.CustomerMessages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Controllers;

[ApiController]
[Route("api/customer-messages")]
[EnableCors("CustomerMessageRealtime")]
[EnableRateLimiting("CustomerMessageHttp")]
[Authorize(AuthenticationSchemes = CustomerMessageAuthenticationDefaults.Scheme)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CustomerMessagePersistenceController(
    ApplicationDbContext db,
    ICustomerMessageAdminService customerMessageService,
    ICustomerMessageRateLimiter customerMessageRateLimiter) : ControllerBase
{
    [HttpPost("support-messages")]
    public async Task<ActionResult<CustomerRealtimeActionResult>> RecordCustomerMessage(
        [FromBody] CustomerRealtimeCustomerMessageInput? input,
        CancellationToken ct)
    {
        if (input is null)
        {
            return BadRequest(CustomerRealtimeActionResult.Failed("Du lieu tin nhan khong hop le."));
        }

        var customerId = await GetAuthorizedCustomerIdAsync(ct);
        if (!customerId.HasValue)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                CustomerRealtimeActionResult.Forbidden("Khach hang khong hop le."));
        }

        if (!await customerMessageRateLimiter.TryAcquireCustomerSendAsync(customerId.Value, ct))
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                CustomerRealtimeActionResult.Failed("Ban dang gui tin qua nhanh. Vui long thu lai sau it phut."));
        }

        var result = await customerMessageService.RecordCustomerMessageAsync(
            new CustomerMessageCreateModel
            {
                ConversationId = input.ConversationId,
                UserId = customerId.Value,
                Subject = input.Subject,
                ClientMessageId = input.ClientMessageId,
                Body = input.Body,
            },
            ct);
        var payload = CustomerRealtimeActionResult.FromActionResult(result);

        if (!result.Found)
        {
            return NotFound(payload);
        }

        return result.Succeeded ? Ok(payload) : BadRequest(payload);
    }

    [HttpPost("ai-exchanges")]
    public async Task<ActionResult<CustomerRealtimeActionResult>> RecordAiExchange(
        [FromBody] CustomerRealtimeAiExchangeInput? input,
        CancellationToken ct)
    {
        if (input is null)
        {
            return BadRequest(CustomerRealtimeActionResult.Failed("Du lieu trao doi AI khong hop le."));
        }

        var customerId = await GetAuthorizedCustomerIdAsync(ct);
        if (!customerId.HasValue)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                CustomerRealtimeActionResult.Forbidden("Khach hang khong hop le."));
        }

        if (!await customerMessageRateLimiter.TryAcquireCustomerSendAsync(customerId.Value, ct))
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                CustomerRealtimeActionResult.Failed("Ban dang gui tin qua nhanh. Vui long thu lai sau it phut."));
        }

        var result = await customerMessageService.RecordCustomerAiExchangeAsync(
            new CustomerAiExchangeCreateModel
            {
                ConversationId = input.ConversationId,
                UserId = customerId.Value,
                Question = input.Question,
                Reply = input.Reply,
                AiMetadataJson = input.AiMetadataJson,
                Receipt = input.Receipt,
            },
            ct);
        var payload = CustomerRealtimeActionResult.FromActionResult(result);

        if (!result.Found)
        {
            return NotFound(payload);
        }

        return result.Succeeded ? Ok(payload) : BadRequest(payload);
    }

    private async Task<long?> GetAuthorizedCustomerIdAsync(CancellationToken ct)
    {
        var hasCustomerScope = User.Claims.Any(claim =>
            claim.Type == CustomerMessageAuthenticationDefaults.ScopeClaim &&
            string.Equals(
                claim.Value,
                CustomerMessageAuthenticationDefaults.AccessScope,
                StringComparison.Ordinal));
        var rawValue = User.FindFirstValue(CustomerMessageAuthenticationDefaults.CustomerIdClaim);
        if (!hasCustomerScope || !long.TryParse(rawValue, out var customerId))
        {
            return null;
        }

        var exists = await db.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == customerId &&
                user.Role == UserRole.Customer &&
                user.IsActive, ct);

        return exists ? customerId : null;
    }
}
