using e_commerce_web_admin.ViewModels.CustomerMessages;

namespace e_commerce_web_admin.Services.CustomerMessages;

public interface ICustomerMessageAdminService
{
    Task<CustomerMessageIndexViewModel> GetIndexAsync(
        CustomerMessageIndexQuery query,
        CancellationToken ct = default);

    Task<CustomerConversationDetailsViewModel?> GetDetailsAsync(
        long id,
        CancellationToken ct = default);

    Task<CustomerMessageHistoryPageViewModel?> GetMessagesAsync(
        long conversationId,
        long? beforeId,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<CustomerMessageActionResult> SendStaffReplyAsync(
        long conversationId,
        long staffId,
        CustomerMessageReplyViewModel form,
        CancellationToken ct = default);

    Task<CustomerMessageActionResult> UpdateStatusAsync(
        long conversationId,
        long staffId,
        CustomerConversationStatusUpdateViewModel form,
        CancellationToken ct = default);

    Task<CustomerMessageActionResult> RecordCustomerMessageAsync(
        CustomerMessageCreateModel input,
        CancellationToken ct = default);

    Task<CustomerMessageActionResult> RecordCustomerAiExchangeAsync(
        CustomerAiExchangeCreateModel input,
        CancellationToken ct = default);

    Task<CustomerMessageActionResult> MarkConversationReadAsync(
        long conversationId,
        CancellationToken ct = default);
}

public sealed class CustomerMessageCreateModel
{
    public long? ConversationId { get; set; }
    public long UserId { get; set; }
    public string? Subject { get; set; }
    public string? ClientMessageId { get; set; }
    public string Body { get; set; } = string.Empty;
}

public sealed class CustomerAiExchangeCreateModel
{
    public long? ConversationId { get; set; }
    public long UserId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Reply { get; set; } = string.Empty;
    public string? AiMetadataJson { get; set; }
    public string Receipt { get; set; } = string.Empty;
}
