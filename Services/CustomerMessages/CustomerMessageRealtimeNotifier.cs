using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Hubs;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.ViewModels.CustomerMessages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.CustomerMessages;

public sealed class CustomerMessageRealtimeNotifier(
    ApplicationDbContext db,
    IHubContext<CustomerMessageHub> hubContext) : ICustomerMessageRealtimeNotifier
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public Task NotifyMessageSavedAsync(
        long conversationId,
        long messageId,
        CancellationToken ct = default) =>
        NotifyMessagesSavedAsync(conversationId, [messageId], ct);

    public async Task NotifyMessagesSavedAsync(
        long conversationId,
        IReadOnlyCollection<long> messageIds,
        CancellationToken ct = default)
    {
        var conversationPayload = await BuildConversationPayloadAsync(conversationId, ct);
        if (conversationPayload is null)
        {
            return;
        }

        var messages = await db.CustomerMessages
            .AsNoTracking()
            .Include(message => message.User)
            .Include(message => message.Staff)
            .Include(message => message.Conversation)
                .ThenInclude(conversation => conversation!.User)
            .Where(message =>
                message.ConversationId == conversationId &&
                messageIds.Contains(message.Id))
            .OrderBy(message => message.Id)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            var adminPayload = BuildMessagePayload(message, conversationPayload, includeAdminMetadata: true);
            var customerPayload = BuildMessagePayload(
                message,
                ToCustomerConversationPayload(conversationPayload),
                includeAdminMetadata: false);

            await hubContext.Clients
                .Group(CustomerMessageHubGroups.AdminConversation(conversationId))
                .SendAsync(CustomerMessageHubEvents.MessageReceived, adminPayload, ct);
            await hubContext.Clients
                .Group(CustomerMessageHubGroups.CustomerConversation(conversationId))
                .SendAsync(CustomerMessageHubEvents.MessageReceived, customerPayload, ct);
        }

        await SendConversationChangedAsync(conversationPayload, ct);
    }

    public async Task NotifyConversationChangedAsync(
        long conversationId,
        CancellationToken ct = default)
    {
        var payload = await BuildConversationPayloadAsync(conversationId, ct);
        if (payload is not null)
        {
            await SendConversationChangedAsync(payload, ct);
        }
    }

    private async Task SendConversationChangedAsync(
        CustomerRealtimeConversationPayload adminPayload,
        CancellationToken ct)
    {
        var customerPayload = ToCustomerConversationPayload(adminPayload);

        await hubContext.Clients
            .Group(CustomerMessageHubGroups.Admins)
            .SendAsync(CustomerMessageHubEvents.ConversationChanged, adminPayload, ct);
        await hubContext.Clients
            .Group(CustomerMessageHubGroups.Customer(adminPayload.CustomerId))
            .SendAsync(CustomerMessageHubEvents.ConversationChanged, customerPayload, ct);
        await hubContext.Clients
            .Group(CustomerMessageHubGroups.AdminConversation(adminPayload.Id))
            .SendAsync(CustomerMessageHubEvents.ConversationStatusChanged, adminPayload, ct);
        await hubContext.Clients
            .Group(CustomerMessageHubGroups.CustomerConversation(adminPayload.Id))
            .SendAsync(CustomerMessageHubEvents.ConversationStatusChanged, customerPayload, ct);
    }

    private async Task<CustomerRealtimeConversationPayload?> BuildConversationPayloadAsync(
        long conversationId,
        CancellationToken ct)
    {
        var conversation = await db.CustomerConversations
            .AsNoTracking()
            .Where(item => item.Id == conversationId)
            .Select(item => new
            {
                item.Id,
                item.UserId,
                item.Channel,
                CustomerName = item.User != null ? item.User.FullName : "Khách hàng",
                CustomerEmail = item.User != null ? item.User.Email : string.Empty,
                CustomerPhone = item.User != null ? item.User.Phone : null,
                item.Subject,
                item.Status,
                AssignedStaffName = item.AssignedStaff != null ? item.AssignedStaff.FullName : null,
                item.LastMessageAt,
                item.LastCustomerMessageAt,
                item.LastStaffMessageAt,
                item.LastAiMessageAt,
                item.ClosedAt,
            })
            .FirstOrDefaultAsync(ct);
        if (conversation is null)
        {
            return null;
        }

        var stats = await db.CustomerMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .GroupBy(message => message.ConversationId)
            .Select(group => new
            {
                MessageCount = group.Count(),
                UnreadCount = group.Count(message =>
                    message.Sender == CustomerMessageSender.Customer &&
                    !message.IsReadByAdmin),
                AiCount = group.Count(message => message.Sender == CustomerMessageSender.Ai),
            })
            .FirstOrDefaultAsync(ct);
        var lastMessage = await db.CustomerMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.Id)
            .Select(message => new { message.Body, message.Sender })
            .FirstOrDefaultAsync(ct);
        var totalUnread = await db.CustomerMessages
            .AsNoTracking()
            .CountAsync(message =>
                message.Sender == CustomerMessageSender.Customer &&
                !message.IsReadByAdmin, ct);
        var lastSender = lastMessage?.Sender ?? CustomerMessageSender.Customer;

        return new CustomerRealtimeConversationPayload
        {
            Id = conversation.Id,
            CustomerId = conversation.UserId,
            Channel = conversation.Channel.ToString(),
            CustomerName = conversation.CustomerName,
            CustomerEmail = conversation.CustomerEmail,
            CustomerPhone = conversation.CustomerPhone,
            Subject = CustomerMessageDisplay.GetSubjectDisplay(conversation.Subject),
            Status = conversation.Status.ToString(),
            StatusLabel = CustomerMessageDisplay.GetStatusLabel(conversation.Status),
            StatusClass = CustomerMessageDisplay.GetStatusClass(conversation.Status),
            AssignedStaffName = conversation.AssignedStaffName,
            LastMessagePreview = TrimText(lastMessage?.Body ?? string.Empty, 140),
            LastMessageSender = lastSender.ToString(),
            LastMessageSenderLabel = CustomerMessageDisplay.GetSenderLabel(lastSender),
            LastMessageSenderClass = CustomerMessageDisplay.GetSenderClass(lastSender),
            MessageCount = stats?.MessageCount ?? 0,
            UnreadCustomerMessageCount = stats?.UnreadCount ?? 0,
            TotalUnreadCustomerMessageCount = totalUnread,
            AiMessageCount = stats?.AiCount ?? 0,
            LastMessageAtIso = conversation.LastMessageAt.ToString("O", CultureInfo.InvariantCulture),
            LastMessageAtText = FormatDate(conversation.LastMessageAt),
            LastCustomerMessageAtText = FormatNullableDate(conversation.LastCustomerMessageAt),
            LastStaffMessageAtText = FormatNullableDate(conversation.LastStaffMessageAt),
            LastAiMessageAtText = FormatNullableDate(conversation.LastAiMessageAt),
            ClosedAtText = FormatNullableDate(conversation.ClosedAt),
        };
    }

    private static CustomerRealtimeMessagePayload BuildMessagePayload(
        CustomerMessage message,
        CustomerRealtimeConversationPayload conversation,
        bool includeAdminMetadata) => new()
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Sender = message.Sender.ToString(),
            SenderLabel = CustomerMessageDisplay.GetSenderLabel(message.Sender),
            SenderClass = CustomerMessageDisplay.GetSenderClass(message.Sender),
            SenderName = GetSenderName(message),
            Body = message.Body,
            IsReadByAdmin = message.IsReadByAdmin,
            AiProvider = message.AiProvider,
            AiModel = message.AiModel,
            AiPrompt = includeAdminMetadata ? message.AiPrompt : null,
            AiResponseId = includeAdminMetadata ? message.AiResponseId : null,
            AiMetadataJson = includeAdminMetadata
            ? message.AiMetadataJson
            : SanitizeCustomerMetadata(message.AiMetadataJson),
            CreatedAtIso = message.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
            CreatedAtText = FormatDate(message.CreatedAt),
            Conversation = conversation,
        };

    private static CustomerRealtimeConversationPayload ToCustomerConversationPayload(
        CustomerRealtimeConversationPayload payload) => new()
        {
            Id = payload.Id,
            CustomerId = payload.CustomerId,
            Channel = payload.Channel,
            CustomerName = payload.CustomerName,
            Subject = payload.Subject,
            Status = payload.Status,
            StatusLabel = payload.StatusLabel,
            StatusClass = payload.StatusClass,
            AssignedStaffName = payload.AssignedStaffName,
            LastMessagePreview = payload.LastMessagePreview,
            LastMessageSender = payload.LastMessageSender,
            LastMessageSenderLabel = payload.LastMessageSenderLabel,
            LastMessageSenderClass = payload.LastMessageSenderClass,
            MessageCount = payload.MessageCount,
            AiMessageCount = payload.AiMessageCount,
            LastMessageAtIso = payload.LastMessageAtIso,
            LastMessageAtText = payload.LastMessageAtText,
            LastCustomerMessageAtText = payload.LastCustomerMessageAtText,
            LastStaffMessageAtText = payload.LastStaffMessageAtText,
            LastAiMessageAtText = payload.LastAiMessageAtText,
            ClosedAtText = payload.ClosedAtText,
        };

    private static string? SanitizeCustomerMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            var source = JsonNode.Parse(metadataJson) as JsonObject;
            if (source?["products"] is not JsonArray products)
            {
                return null;
            }

            var safeProducts = new JsonArray();
            foreach (var node in products.OfType<JsonObject>().Take(12))
            {
                var safeProduct = new JsonObject();
                CopyValue(node, safeProduct, "id");
                CopyValue(node, safeProduct, "name");
                CopyValue(node, safeProduct, "price");
                CopyValue(node, safeProduct, "imageUrl");
                CopyValue(node, safeProduct, "categoryName");
                CopyValue(node, safeProduct, "detailUrl");
                safeProducts.Add(safeProduct);
            }

            return new JsonObject
            {
                ["source"] = "customer-ai-assistant",
                ["products"] = safeProducts,
            }.ToJsonString(WebJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void CopyValue(JsonObject source, JsonObject target, string propertyName)
    {
        if (source[propertyName] is { } value)
        {
            target[propertyName] = value.DeepClone();
        }
    }

    private static string GetSenderName(CustomerMessage message) => message.Sender switch
    {
        CustomerMessageSender.Customer => message.User?.FullName ??
            message.Conversation?.User?.FullName ??
            "Khách hàng",
        CustomerMessageSender.Staff => message.Staff?.FullName ?? "Admin",
        CustomerMessageSender.Ai => string.IsNullOrWhiteSpace(message.AiModel)
            ? "AI"
            : $"AI ({message.AiModel})",
        _ => "Không rõ",
    };

    private static string FormatDate(DateTime value)
    {
        var vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var vnTime = TimeZoneInfo.ConvertTimeFromUtc(
            value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime(),
            vnZone);
        return vnTime.ToString("dd/MM/yyyy HH:mm", ViCulture);
    }

    private static string? FormatNullableDate(DateTime? value) =>
        value.HasValue ? FormatDate(value.Value) : null;

    private static string TrimText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : string.Concat(normalized.AsSpan(0, Math.Max(0, maxLength - 3)), "...");
    }
}
