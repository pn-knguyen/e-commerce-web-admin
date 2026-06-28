using System.ComponentModel.DataAnnotations;
using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.ViewModels.CustomerMessages;

public sealed class CustomerMessageIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Ai { get; set; }
    public bool Unread { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class CustomerMessageIndexViewModel
{
    public List<CustomerConversationRowViewModel> Conversations { get; set; } = [];

    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Ai { get; set; }
    public bool Unread { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int UnreadCustomerMessageCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        !string.IsNullOrWhiteSpace(Status) ||
        !string.IsNullOrWhiteSpace(Ai) ||
        Unread;
}

public sealed class CustomerMessageWorkspaceViewModel
{
    public CustomerMessageIndexViewModel Index { get; set; } = new();
    public CustomerConversationDetailsViewModel? Conversation { get; set; }
    public bool IsListOnly { get; set; }
}

public sealed class CustomerConversationRowViewModel
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? Subject { get; set; }
    public CustomerConversationStatus Status { get; set; }
    public string? AssignedStaffName { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    public CustomerMessageSender LastMessageSender { get; set; }
    public int MessageCount { get; set; }
    public int UnreadCustomerMessageCount { get; set; }
    public int AiMessageCount { get; set; }
    public DateTime LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CustomerConversationDetailsViewModel
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? Subject { get; set; }
    public CustomerConversationChannel Channel { get; set; }
    public CustomerConversationStatus Status { get; set; }
    public string? AssignedStaffName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public DateTime? LastCustomerMessageAt { get; set; }
    public DateTime? LastStaffMessageAt { get; set; }
    public DateTime? LastAiMessageAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int MessageCount { get; set; }
    public int UnreadCustomerMessageCount { get; set; }
    public int AiMessageCount { get; set; }
    public List<CustomerMessageRowViewModel> Messages { get; set; } = [];
    public bool HasOlderMessages { get; set; }
    public long? OldestMessageId { get; set; }
    public List<CustomerMessageFilterOption> StatusOptions { get; set; } = [];
    public CustomerMessageReplyViewModel ReplyForm { get; set; } = new();
}

public sealed class CustomerMessageHistoryPageViewModel
{
    public List<CustomerMessageRowViewModel> Messages { get; set; } = [];
    public bool HasMore { get; set; }
    public long? NextBeforeId { get; set; }
}

public sealed class CustomerMessageRowViewModel
{
    public long Id { get; set; }
    public CustomerMessageSender Sender { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsReadByAdmin { get; set; }
    public string? AiProvider { get; set; }
    public string? AiModel { get; set; }
    public string? AiPrompt { get; set; }
    public string? AiResponseId { get; set; }
    public string? AiMetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CustomerMessageReplyViewModel
{
    public long ConversationId { get; set; }

    [StringLength(64, ErrorMessage = "Mã gửi tin tối đa 64 ký tự.")]
    public string? ClientMessageId { get; set; }

    [Required(ErrorMessage = "Nội dung phản hồi là bắt buộc.")]
    [StringLength(4000, ErrorMessage = "Nội dung phản hồi tối đa 4000 ký tự.")]
    public string Body { get; set; } = string.Empty;
}

public sealed class CustomerConversationStatusUpdateViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Trạng thái hội thoại là bắt buộc.")]
    public CustomerConversationStatus Status { get; set; }
}

public sealed class CustomerMessageFilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public sealed class CustomerMessageActionResult
{
    public bool Found { get; init; } = true;
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public long? ConversationId { get; init; }
    public long? MessageId { get; init; }
    public List<CustomerMessageValidationError> Errors { get; init; } = [];

    public static CustomerMessageActionResult NotFound() => new() { Found = false };

    public static CustomerMessageActionResult Failed(params CustomerMessageValidationError[] errors) =>
        new()
        {
            Succeeded = false,
            Message = errors.FirstOrDefault()?.Message ?? "Dữ liệu không hợp lệ.",
            Errors = errors.ToList(),
        };

    public static CustomerMessageActionResult Success(
        string message,
        long? conversationId = null,
        long? messageId = null) =>
        new()
        {
            Succeeded = true,
            Message = message,
            ConversationId = conversationId,
            MessageId = messageId,
        };
}

public sealed record CustomerMessageValidationError(string FieldName, string Message);

public static class CustomerMessageDisplay
{
    public static string GetStatusLabel(CustomerConversationStatus status) => status switch
    {
        CustomerConversationStatus.Open => "Đang mở",
        CustomerConversationStatus.AwaitingCustomer => "Chờ khách phản hồi",
        CustomerConversationStatus.Closed => "Đã đóng",
        _ => "Không xác định",
    };

    public static string GetStatusClass(CustomerConversationStatus status) => status switch
    {
        CustomerConversationStatus.Open => "is-open",
        CustomerConversationStatus.AwaitingCustomer => "is-awaiting",
        CustomerConversationStatus.Closed => "is-closed",
        _ => "is-muted",
    };

    public static string GetSenderLabel(CustomerMessageSender sender) => sender switch
    {
        CustomerMessageSender.Customer => "Khách hàng",
        CustomerMessageSender.Staff => "Admin",
        CustomerMessageSender.Ai => "AI",
        _ => "Không rõ",
    };

    public static string GetSenderClass(CustomerMessageSender sender) => sender switch
    {
        CustomerMessageSender.Customer => "is-customer",
        CustomerMessageSender.Staff => "is-staff",
        CustomerMessageSender.Ai => "is-ai",
        _ => "is-muted",
    };

    public static string GetSubjectDisplay(string? subject) =>
        string.IsNullOrWhiteSpace(subject) ? "Hội thoại không tiêu đề" : subject.Trim();
}
