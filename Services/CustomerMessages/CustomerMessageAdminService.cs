using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.ViewModels.CustomerMessages;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.CustomerMessages;

public sealed partial class CustomerMessageAdminService : ICustomerMessageAdminService
{
    private const int DefaultPageSize = 20;
    private const int DefaultMessagePageSize = 50;
    private const int MaxMessagePageSize = 100;
    private const int ReplyMaxLength = 4000;
    private const int PromptMaxLength = 8000;
    private const int MetadataMaxLength = 16000;
    private const int SubjectMaxLength = 255;
    private const int ClientMessageIdMaxLength = 64;

    private readonly ApplicationDbContext db;
    private readonly ICustomerMessageRealtimeNotifier realtimeNotifier;
    private readonly ICustomerAiReceiptValidator receiptValidator;

    public CustomerMessageAdminService(
        ApplicationDbContext db,
        ICustomerMessageRealtimeNotifier realtimeNotifier,
        ICustomerAiReceiptValidator receiptValidator)
    {
        this.db = db;
        this.realtimeNotifier = realtimeNotifier;
        this.receiptValidator = receiptValidator;
    }

    public async Task<CustomerMessageIndexViewModel> GetIndexAsync(
        CustomerMessageIndexQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var baseQuery = db.CustomerConversations.AsNoTracking();
        var filteredQuery = ApplyFilters(baseQuery, query);
        var totalCount = await filteredQuery.CountAsync(ct);
        var unreadCustomerMessageCount = await db.CustomerMessages
            .AsNoTracking()
            .CountAsync(message =>
                message.Sender == CustomerMessageSender.Customer &&
                !message.IsReadByAdmin, ct);

        var rows = await filteredQuery
            .OrderByDescending(conversation => conversation.Messages.Any(message =>
                message.Sender == CustomerMessageSender.Customer &&
                !message.IsReadByAdmin))
            .ThenByDescending(conversation => conversation.LastMessageAt)
            .ThenByDescending(conversation => conversation.Id)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(conversation => new CustomerConversationRowViewModel
            {
                Id = conversation.Id,
                CustomerName = conversation.User != null ? conversation.User.FullName : "Khách hàng",
                CustomerEmail = conversation.User != null ? conversation.User.Email : string.Empty,
                CustomerPhone = conversation.User != null ? conversation.User.Phone : null,
                Subject = conversation.Subject,
                Status = conversation.Status,
                AssignedStaffName = conversation.AssignedStaff != null ? conversation.AssignedStaff.FullName : null,
                LastMessagePreview = conversation.Messages
                    .OrderByDescending(message => message.Id)
                    .Select(message => message.Body)
                    .FirstOrDefault() ?? string.Empty,
                LastMessageSender = conversation.Messages
                    .OrderByDescending(message => message.Id)
                    .Select(message => message.Sender)
                    .FirstOrDefault(),
                MessageCount = conversation.Messages.Count,
                UnreadCustomerMessageCount = conversation.Messages.Count(message =>
                    message.Sender == CustomerMessageSender.Customer &&
                    !message.IsReadByAdmin),
                AiMessageCount = conversation.Messages.Count(message => message.Sender == CustomerMessageSender.Ai),
                LastMessageAt = conversation.LastMessageAt,
                CreatedAt = conversation.CreatedAt,
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            row.LastMessagePreview = TrimText(row.LastMessagePreview, 140);
        }

        return new CustomerMessageIndexViewModel
        {
            Conversations = rows,
            Search = query.Search?.Trim(),
            Status = NormalizeStatus(query.Status)?.ToString(),
            Ai = NormalizeAiFilter(query.Ai),
            Unread = query.Unread,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            UnreadCustomerMessageCount = unreadCustomerMessageCount,
        };
    }

    public async Task<CustomerConversationDetailsViewModel?> GetDetailsAsync(
        long id,
        CancellationToken ct = default)
    {
        var viewModel = await db.CustomerConversations
            .AsNoTracking()
            .Where(conversation => conversation.Id == id)
            .Select(conversation => new CustomerConversationDetailsViewModel
            {
                Id = conversation.Id,
                CustomerId = conversation.UserId,
                CustomerName = conversation.User != null ? conversation.User.FullName : "Khách hàng",
                CustomerEmail = conversation.User != null ? conversation.User.Email : string.Empty,
                CustomerPhone = conversation.User != null ? conversation.User.Phone : null,
                Subject = conversation.Subject,
                Channel = conversation.Channel,
                Status = conversation.Status,
                AssignedStaffName = conversation.AssignedStaff != null ? conversation.AssignedStaff.FullName : null,
                CreatedAt = conversation.CreatedAt,
                LastMessageAt = conversation.LastMessageAt,
                LastCustomerMessageAt = conversation.LastCustomerMessageAt,
                LastStaffMessageAt = conversation.LastStaffMessageAt,
                LastAiMessageAt = conversation.LastAiMessageAt,
                ClosedAt = conversation.ClosedAt,
                MessageCount = conversation.Messages.Count,
                UnreadCustomerMessageCount = conversation.Messages.Count(message =>
                    message.Sender == CustomerMessageSender.Customer &&
                    !message.IsReadByAdmin),
                AiMessageCount = conversation.Messages.Count(message => message.Sender == CustomerMessageSender.Ai),
            })
            .FirstOrDefaultAsync(ct);
        if (viewModel is null)
        {
            return null;
        }

        await MarkConversationReadAsync(id, ct);
        viewModel.UnreadCustomerMessageCount = 0;

        var messagePage = await GetMessagesAsync(id, null, DefaultMessagePageSize, ct);
        viewModel.Messages = messagePage?.Messages ?? [];
        viewModel.HasOlderMessages = messagePage?.HasMore == true;
        viewModel.OldestMessageId = messagePage?.NextBeforeId;
        viewModel.StatusOptions = BuildStatusOptions(viewModel.Status);
        viewModel.ReplyForm = new CustomerMessageReplyViewModel
        {
            ConversationId = id,
            ClientMessageId = Guid.NewGuid().ToString("N"),
        };

        return viewModel;
    }

    public async Task<CustomerMessageHistoryPageViewModel?> GetMessagesAsync(
        long conversationId,
        long? beforeId,
        int pageSize = DefaultMessagePageSize,
        CancellationToken ct = default)
    {
        if (!await db.CustomerConversations
                .AsNoTracking()
                .AnyAsync(conversation => conversation.Id == conversationId, ct))
        {
            return null;
        }

        var take = Math.Clamp(pageSize, 1, MaxMessagePageSize);
        var query = db.CustomerMessages
            .AsNoTracking()
            .Include(message => message.User)
            .Include(message => message.Staff)
            .Where(message => message.ConversationId == conversationId);
        if (beforeId.HasValue)
        {
            query = query.Where(message => message.Id < beforeId.Value);
        }

        var entities = await query
            .OrderByDescending(message => message.Id)
            .Take(take + 1)
            .ToListAsync(ct);
        var hasMore = entities.Count > take;
        if (hasMore)
        {
            entities.RemoveAt(entities.Count - 1);
        }

        entities.Reverse();
        var customerName = await db.CustomerConversations
            .AsNoTracking()
            .Where(conversation => conversation.Id == conversationId)
            .Select(conversation => conversation.User != null ? conversation.User.FullName : "Khách hàng")
            .FirstAsync(ct);
        var messages = entities.Select(message => new CustomerMessageRowViewModel
        {
            Id = message.Id,
            Sender = message.Sender,
            SenderName = GetSenderName(message, customerName),
            Body = message.Body,
            IsReadByAdmin = message.IsReadByAdmin,
            AiProvider = message.AiProvider,
            AiModel = message.AiModel,
            AiPrompt = message.AiPrompt,
            AiResponseId = message.AiResponseId,
            AiMetadataJson = message.AiMetadataJson,
            CreatedAt = message.CreatedAt,
        }).ToList();

        return new CustomerMessageHistoryPageViewModel
        {
            Messages = messages,
            HasMore = hasMore,
            NextBeforeId = messages.FirstOrDefault()?.Id,
        };
    }

    private static IQueryable<CustomerConversation> ApplyFilters(
        IQueryable<CustomerConversation> query,
        CustomerMessageIndexQuery filters)
    {
        var search = filters.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(conversation =>
                (conversation.Subject != null && conversation.Subject.Contains(search)) ||
                (conversation.User != null &&
                    (conversation.User.FullName.Contains(search) ||
                        conversation.User.Email.Contains(search) ||
                        (conversation.User.Phone != null && conversation.User.Phone.Contains(search)))) ||
                conversation.Messages.Any(message => message.Body.Contains(search)));
        }

        var status = NormalizeStatus(filters.Status);
        if (status.HasValue)
        {
            query = query.Where(conversation => conversation.Status == status.Value);
        }

        if (filters.Unread)
        {
            query = query.Where(conversation => conversation.Messages.Any(message =>
                message.Sender == CustomerMessageSender.Customer &&
                !message.IsReadByAdmin));
        }

        var aiFilter = NormalizeAiFilter(filters.Ai);
        if (aiFilter == "with-ai")
        {
            query = query.Where(conversation =>
                conversation.Messages.Any(message => message.Sender == CustomerMessageSender.Ai));
        }
        else if (aiFilter == "without-ai")
        {
            query = query.Where(conversation =>
                !conversation.Messages.Any(message => message.Sender == CustomerMessageSender.Ai));
        }

        return query;
    }

    private static CustomerConversationStatus? NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<CustomerConversationStatus>(value.Trim(), true, out var status) &&
               Enum.IsDefined(status)
            ? status
            : null;
    }

    private static string? NormalizeAiFilter(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "with-ai" => "with-ai",
            "without-ai" => "without-ai",
            _ => null,
        };

    private static List<CustomerMessageFilterOption> BuildStatusOptions(
        CustomerConversationStatus selectedStatus) =>
        Enum.GetValues<CustomerConversationStatus>()
            .Select(status => new CustomerMessageFilterOption
            {
                Value = status.ToString(),
                Text = CustomerMessageDisplay.GetStatusLabel(status),
                Selected = selectedStatus == status,
            })
            .ToList();

    private static string GetSenderName(CustomerMessage message, string? customerName) =>
        message.Sender switch
        {
            CustomerMessageSender.Customer => message.User?.FullName ?? customerName ?? "Khách hàng",
            CustomerMessageSender.Staff => message.Staff?.FullName ?? "Admin",
            CustomerMessageSender.Ai => string.IsNullOrWhiteSpace(message.AiModel)
                ? "AI"
                : $"AI ({message.AiModel})",
            _ => "Không rõ",
        };

    private static string NormalizeSubject(string? subject, string fallbackBody)
    {
        var candidate = string.IsNullOrWhiteSpace(subject) ? fallbackBody.Trim() : subject.Trim();
        return TrimText(candidate, SubjectMaxLength);
    }

    private static string TrimText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : string.Concat(normalized.AsSpan(0, Math.Max(0, maxLength - 3)), "...");
    }

    private static string? TrimNullable(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : TrimText(value, maxLength);
}
