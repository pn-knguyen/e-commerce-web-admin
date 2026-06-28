namespace e_commerce_web_admin.Services.CustomerMessages;

public interface ICustomerMessageRealtimeNotifier
{
    Task NotifyMessageSavedAsync(
        long conversationId,
        long messageId,
        CancellationToken ct = default);

    Task NotifyMessagesSavedAsync(
        long conversationId,
        IReadOnlyCollection<long> messageIds,
        CancellationToken ct = default);

    Task NotifyConversationChangedAsync(
        long conversationId,
        CancellationToken ct = default);
}
