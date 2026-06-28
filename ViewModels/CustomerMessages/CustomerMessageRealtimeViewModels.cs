using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.CustomerMessages;

public sealed class CustomerRealtimeCustomerMessageInput
{
    public long? ConversationId { get; set; }

    [StringLength(255)]
    public string? Subject { get; set; }

    [StringLength(64)]
    public string? ClientMessageId { get; set; }

    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = string.Empty;
}

public sealed class CustomerRealtimeStaffReplyInput
{
    public long ConversationId { get; set; }

    [StringLength(64)]
    public string? ClientMessageId { get; set; }

    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = string.Empty;
}

public sealed class CustomerRealtimeAiExchangeInput
{
    public long? ConversationId { get; set; }

    [Required]
    [StringLength(4000)]
    public string Question { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Reply { get; set; } = string.Empty;

    [StringLength(16000)]
    public string? AiMetadataJson { get; set; }

    [Required]
    public string Receipt { get; set; } = string.Empty;
}

public sealed class CustomerRealtimeActionResult
{
    public bool Found { get; init; } = true;
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public long? ConversationId { get; init; }
    public long? MessageId { get; init; }
    public List<CustomerMessageValidationError> Errors { get; init; } = [];

    public static CustomerRealtimeActionResult FromActionResult(CustomerMessageActionResult result) =>
        new()
        {
            Found = result.Found,
            Succeeded = result.Succeeded,
            Message = result.Message,
            ConversationId = result.ConversationId,
            MessageId = result.MessageId,
            Errors = result.Errors,
        };

    public static CustomerRealtimeActionResult Forbidden(string message) =>
        new() { Succeeded = false, Message = message };

    public static CustomerRealtimeActionResult Failed(
        string message,
        params CustomerMessageValidationError[] errors) =>
        new()
        {
            Succeeded = false,
            Message = message,
            Errors = errors.ToList(),
        };
}

public sealed class CustomerRealtimeConversationPayload
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public string? AssignedStaffName { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    public string LastMessageSender { get; set; } = string.Empty;
    public string LastMessageSenderLabel { get; set; } = string.Empty;
    public string LastMessageSenderClass { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public int UnreadCustomerMessageCount { get; set; }
    public int TotalUnreadCustomerMessageCount { get; set; }
    public int AiMessageCount { get; set; }
    public string LastMessageAtIso { get; set; } = string.Empty;
    public string LastMessageAtText { get; set; } = string.Empty;
    public string? LastCustomerMessageAtText { get; set; }
    public string? LastStaffMessageAtText { get; set; }
    public string? LastAiMessageAtText { get; set; }
    public string? ClosedAtText { get; set; }
}

public sealed class CustomerRealtimeMessagePayload
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string SenderLabel { get; set; } = string.Empty;
    public string SenderClass { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsReadByAdmin { get; set; }
    public string? AiProvider { get; set; }
    public string? AiModel { get; set; }
    public string? AiPrompt { get; set; }
    public string? AiResponseId { get; set; }
    public string? AiMetadataJson { get; set; }
    public string CreatedAtIso { get; set; } = string.Empty;
    public string CreatedAtText { get; set; } = string.Empty;
    public CustomerRealtimeConversationPayload Conversation { get; set; } = new();
}
