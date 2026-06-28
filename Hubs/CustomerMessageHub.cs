using System.Security.Claims;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Services.CustomerMessages;
using e_commerce_web_admin.ViewModels.CustomerMessages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Hubs;

public static class CustomerMessageHubGroups
{
    public const string Admins = "customer-message-admins";

    public static string AdminConversation(long conversationId) =>
        $"customer-message-admin-conversation:{conversationId}";

    public static string CustomerConversation(long conversationId) =>
        $"customer-message-customer-conversation:{conversationId}";

    public static string Customer(long customerId) =>
        $"customer-message-customer:{customerId}";
}

public static class CustomerMessageHubEvents
{
    public const string MessageReceived = "MessageReceived";
    public const string ConversationChanged = "ConversationChanged";
    public const string ConversationStatusChanged = "ConversationStatusChanged";
}

public sealed class CustomerMessageHub(
    ApplicationDbContext db,
    ICustomerMessageAdminService customerMessageService,
    ICustomerMessageRateLimiter customerMessageRateLimiter,
    UserManager<Staff> userManager,
    RoleManager<IdentityRole<long>> roleManager) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var staff = await GetActiveStaffAsync();
        if (staff is not null && await HasPermissionAsync(staff, Permissions.View))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, CustomerMessageHubGroups.Admins);
        }

        var customerId = await GetAuthorizedCustomerIdAsync();
        if (customerId.HasValue)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                CustomerMessageHubGroups.Customer(customerId.Value));
        }

        await base.OnConnectedAsync();
    }

    public async Task<CustomerRealtimeActionResult> JoinConversation(long conversationId)
    {
        if (conversationId <= 0)
        {
            return CustomerRealtimeActionResult.Failed("Hội thoại không hợp lệ.");
        }

        var staff = await GetActiveStaffAsync();
        if (staff is not null && await HasPermissionAsync(staff, Permissions.View))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                CustomerMessageHubGroups.AdminConversation(conversationId));
            return Connected(conversationId);
        }

        var customerId = await GetAuthorizedCustomerIdAsync();
        var ownsConversation = customerId.HasValue && await db.CustomerConversations
            .AsNoTracking()
            .AnyAsync(conversation =>
                conversation.Id == conversationId &&
                conversation.UserId == customerId.Value,
                Context.ConnectionAborted);
        if (!ownsConversation)
        {
            return CustomerRealtimeActionResult.Forbidden("Bạn không có quyền xem hội thoại này.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            CustomerMessageHubGroups.CustomerConversation(conversationId));

        return Connected(conversationId);
    }

    public async Task<CustomerRealtimeActionResult> LeaveConversation(long conversationId)
    {
        if (conversationId <= 0)
        {
            return CustomerRealtimeActionResult.Failed("Hoi thoai khong hop le.");
        }

        var staff = await GetActiveStaffAsync();
        if (staff is null || !await HasPermissionAsync(staff, Permissions.View))
        {
            return CustomerRealtimeActionResult.Forbidden("Ban khong co quyen roi hoi thoai nay.");
        }

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            CustomerMessageHubGroups.AdminConversation(conversationId));

        return new CustomerRealtimeActionResult
        {
            Succeeded = true,
            ConversationId = conversationId,
            Message = "Da roi hoi thoai realtime.",
        };
    }

    public async Task<CustomerRealtimeActionResult> SendCustomerMessage(
        CustomerRealtimeCustomerMessageInput? input)
    {
        if (input is null)
        {
            return CustomerRealtimeActionResult.Failed("Dữ liệu tin nhắn không hợp lệ.");
        }

        var customerId = await GetAuthorizedCustomerIdAsync();
        if (!customerId.HasValue)
        {
            return CustomerRealtimeActionResult.Forbidden("Khách hàng không hợp lệ.");
        }

        if (!await customerMessageRateLimiter.TryAcquireCustomerSendAsync(
                customerId.Value,
                Context.ConnectionAborted))
        {
            return CustomerRealtimeActionResult.Failed("Bạn đang gửi tin quá nhanh. Vui lòng thử lại sau ít phút.");
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
            Context.ConnectionAborted);

        return CustomerRealtimeActionResult.FromActionResult(result);
    }

    public async Task<CustomerRealtimeActionResult> SendStaffReply(
        CustomerRealtimeStaffReplyInput? input)
    {
        if (input is null)
        {
            return CustomerRealtimeActionResult.Failed("Dữ liệu phản hồi không hợp lệ.");
        }

        var staff = await GetActiveStaffAsync();
        if (staff is null || !await HasPermissionAsync(staff, Permissions.Edit))
        {
            return CustomerRealtimeActionResult.Forbidden("Bạn không có quyền phản hồi hội thoại này.");
        }

        var result = await customerMessageService.SendStaffReplyAsync(
            input.ConversationId,
            staff.Id,
            new CustomerMessageReplyViewModel
            {
                ConversationId = input.ConversationId,
                ClientMessageId = input.ClientMessageId,
                Body = input.Body,
            },
            Context.ConnectionAborted);

        return CustomerRealtimeActionResult.FromActionResult(result);
    }

    public async Task<CustomerRealtimeActionResult> RecordCustomerAiExchange(
        CustomerRealtimeAiExchangeInput? input)
    {
        if (input is null)
        {
            return CustomerRealtimeActionResult.Failed("Dữ liệu trao đổi AI không hợp lệ.");
        }

        var customerId = await GetAuthorizedCustomerIdAsync();
        if (!customerId.HasValue)
        {
            return CustomerRealtimeActionResult.Forbidden("Khách hàng không hợp lệ.");
        }

        if (!await customerMessageRateLimiter.TryAcquireCustomerSendAsync(
                customerId.Value,
                Context.ConnectionAborted))
        {
            return CustomerRealtimeActionResult.Failed("Bạn đang gửi tin quá nhanh. Vui lòng thử lại sau ít phút.");
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
            Context.ConnectionAborted);

        return CustomerRealtimeActionResult.FromActionResult(result);
    }

    public async Task<CustomerRealtimeActionResult> MarkConversationRead(long conversationId)
    {
        if (conversationId <= 0)
        {
            return CustomerRealtimeActionResult.Failed("Hội thoại không hợp lệ.");
        }

        var staff = await GetActiveStaffAsync();
        if (staff is null || !await HasPermissionAsync(staff, Permissions.View))
        {
            return CustomerRealtimeActionResult.Forbidden("Bạn không có quyền cập nhật hội thoại này.");
        }

        var result = await customerMessageService.MarkConversationReadAsync(
            conversationId,
            Context.ConnectionAborted);
        return CustomerRealtimeActionResult.FromActionResult(result);
    }

    private async Task<Staff?> GetActiveStaffAsync()
    {
        var staffIdentity = Context.User?.Identities.FirstOrDefault(identity =>
            identity.IsAuthenticated &&
            string.Equals(
                identity.AuthenticationType,
                IdentityConstants.ApplicationScheme,
                StringComparison.Ordinal));
        if (staffIdentity is null)
        {
            return null;
        }

        var staff = await userManager.GetUserAsync(new ClaimsPrincipal(staffIdentity));
        return staff?.IsActive == true ? staff : null;
    }

    private async Task<bool> HasPermissionAsync(Staff staff, string permission)
    {
        if (await userManager.IsInRoleAsync(staff, StaffRoleNames.Admin))
        {
            return true;
        }

        var permissionValue = Permissions.Build("CustomerMessages", permission);
        var roleNames = await userManager.GetRolesAsync(staff);
        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var claims = await roleManager.GetClaimsAsync(role);
            if (claims.Any(claim =>
                    claim.Type == StaffClaimTypes.Permission &&
                    string.Equals(claim.Value, permissionValue, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<long?> GetAuthorizedCustomerIdAsync()
    {
        var hasCustomerScope = Context.User?.Claims.Any(claim =>
            claim.Type == CustomerMessageAuthenticationDefaults.ScopeClaim &&
            claim.Value == CustomerMessageAuthenticationDefaults.AccessScope) == true;
        var rawValue = Context.User?.FindFirst(
            CustomerMessageAuthenticationDefaults.CustomerIdClaim)?.Value;
        if (!hasCustomerScope || !long.TryParse(rawValue, out var customerId))
        {
            return null;
        }

        var exists = await db.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == customerId &&
                user.Role == UserRole.Customer &&
                user.IsActive,
                Context.ConnectionAborted);

        return exists ? customerId : null;
    }

    private static CustomerRealtimeActionResult Connected(long conversationId) => new()
    {
        Succeeded = true,
        ConversationId = conversationId,
        Message = "Đã kết nối hội thoại realtime.",
    };
}
