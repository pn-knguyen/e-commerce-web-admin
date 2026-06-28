using System.Text.Json;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.ViewModels.CustomerMessages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.CustomerMessages;

public sealed partial class CustomerMessageAdminService
{
    public async Task<CustomerMessageActionResult> SendStaffReplyAsync(
        long conversationId,
        long staffId,
        CustomerMessageReplyViewModel form,
        CancellationToken ct = default)
    {
        var body = form.Body?.Trim() ?? string.Empty;
        var bodyError = ValidateMessage(body, nameof(form.Body), "Nội dung phản hồi");
        if (bodyError is not null)
        {
            return CustomerMessageActionResult.Failed(bodyError);
        }

        var clientMessageId = NormalizeClientMessageId(form.ClientMessageId);
        var clientMessageIdError = ValidateClientMessageId(
            clientMessageId,
            nameof(form.ClientMessageId),
            required: true);
        if (clientMessageIdError is not null)
        {
            return CustomerMessageActionResult.Failed(clientMessageIdError);
        }

        var existingResult = await TryResolveExistingStaffMessageAsync(
            conversationId,
            staffId,
            clientMessageId,
            body,
            ct);
        if (existingResult is not null)
        {
            await NotifySavedMessageAsync(existingResult, ct);
            return existingResult;
        }

        var channel = await db.CustomerConversations
            .AsNoTracking()
            .Where(conversation => conversation.Id == conversationId)
            .Select(conversation => (CustomerConversationChannel?)conversation.Channel)
            .FirstOrDefaultAsync(ct);
        if (!channel.HasValue)
        {
            return CustomerMessageActionResult.NotFound();
        }

        if (channel == CustomerConversationChannel.Ai)
        {
            return CustomerMessageActionResult.Failed(
                new CustomerMessageValidationError(
                    nameof(form.Body),
                    "Hội thoại AI chỉ dùng để theo dõi, admin không thể phản hồi."));
        }

        var now = DateTime.UtcNow;
        var executionStrategy = db.Database.CreateExecutionStrategy();
        var result = await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var updated = await db.CustomerConversations
                .Where(conversation =>
                    conversation.Id == conversationId &&
                    conversation.Channel == CustomerConversationChannel.Support)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(conversation => conversation.AssignedStaffId,
                        conversation => conversation.AssignedStaffId ?? staffId)
                    .SetProperty(conversation => conversation.Status, CustomerConversationStatus.AwaitingCustomer)
                    .SetProperty(conversation => conversation.LastMessageAt,
                        conversation => conversation.LastMessageAt < now ? now : conversation.LastMessageAt)
                    .SetProperty(conversation => conversation.LastStaffMessageAt,
                        conversation => !conversation.LastStaffMessageAt.HasValue ||
                            conversation.LastStaffMessageAt < now
                                ? now
                                : conversation.LastStaffMessageAt)
                    .SetProperty(conversation => conversation.UpdatedAt, now)
                    .SetProperty(conversation => conversation.ClosedAt, (DateTime?)null), ct);
            if (updated == 0)
            {
                await transaction.RollbackAsync(ct);
                return CustomerMessageActionResult.NotFound();
            }

            var message = new CustomerMessage
            {
                ConversationId = conversationId,
                Sender = CustomerMessageSender.Staff,
                StaffId = staffId,
                ClientMessageId = clientMessageId,
                Body = body,
                IsReadByAdmin = true,
                CreatedAt = now,
            };
            db.CustomerMessages.Add(message);

            try
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException exception) when (
                clientMessageId is not null &&
                IsUniqueConstraintViolation(exception))
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                return await TryResolveExistingStaffMessageAsync(
                        conversationId,
                        staffId,
                        clientMessageId,
                        body,
                        ct) ??
                    CustomerMessageActionResult.Failed(
                        new CustomerMessageValidationError(
                            nameof(form.ClientMessageId),
                            "Mã gửi tin đã được sử dụng."));
            }

            return CustomerMessageActionResult.Success(
                "Đã gửi phản hồi cho khách hàng.",
                conversationId,
                message.Id);
        });

        await NotifySavedMessageAsync(result, ct);
        return result;
    }

    public async Task<CustomerMessageActionResult> UpdateStatusAsync(
        long conversationId,
        long staffId,
        CustomerConversationStatusUpdateViewModel form,
        CancellationToken ct = default)
    {
        if (form.Id != conversationId || !Enum.IsDefined(form.Status))
        {
            return CustomerMessageActionResult.Failed(
                new CustomerMessageValidationError(nameof(form.Status), "Trạng thái hội thoại không hợp lệ."));
        }

        var now = DateTime.UtcNow;
        var updated = await db.CustomerConversations
            .Where(conversation => conversation.Id == conversationId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(conversation => conversation.Status, form.Status)
                .SetProperty(conversation => conversation.AssignedStaffId,
                    conversation => conversation.AssignedStaffId ?? staffId)
                .SetProperty(conversation => conversation.UpdatedAt, now)
                .SetProperty(conversation => conversation.ClosedAt,
                    form.Status == CustomerConversationStatus.Closed ? now : null), ct);
        if (updated == 0)
        {
            return CustomerMessageActionResult.NotFound();
        }

        await realtimeNotifier.NotifyConversationChangedAsync(conversationId, ct);
        return CustomerMessageActionResult.Success(
            $"Đã cập nhật hội thoại sang trạng thái {CustomerMessageDisplay.GetStatusLabel(form.Status).ToLowerInvariant()}.",
            conversationId);
    }

    public async Task<CustomerMessageActionResult> RecordCustomerMessageAsync(
        CustomerMessageCreateModel input,
        CancellationToken ct = default)
    {
        var body = input.Body?.Trim() ?? string.Empty;
        var bodyError = ValidateMessage(body, nameof(input.Body), "Nội dung tin nhắn");
        if (bodyError is not null)
        {
            return CustomerMessageActionResult.Failed(bodyError);
        }

        var clientMessageId = NormalizeClientMessageId(input.ClientMessageId);
        var clientMessageIdError = ValidateClientMessageId(
            clientMessageId,
            nameof(input.ClientMessageId),
            required: true);
        if (clientMessageIdError is not null)
        {
            return CustomerMessageActionResult.Failed(clientMessageIdError);
        }

        var userExists = await db.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == input.UserId &&
                user.Role == UserRole.Customer &&
                user.IsActive, ct);
        if (!userExists)
        {
            return CustomerMessageActionResult.NotFound();
        }

        var existingResult = await TryResolveExistingCustomerMessageAsync(
            input.ConversationId,
            input.UserId,
            clientMessageId,
            body,
            ct);
        if (existingResult is not null)
        {
            await NotifySavedMessageAsync(existingResult, ct);
            return existingResult;
        }

        var now = DateTime.UtcNow;
        var executionStrategy = db.Database.CreateExecutionStrategy();
        var result = await executionStrategy.ExecuteAsync(async () =>
        {
            CustomerConversation conversation;
            CustomerMessage message;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (input.ConversationId.HasValue)
            {
                var normalizedSubject = NormalizeSubject(input.Subject, body);
                var updated = await db.CustomerConversations
                    .Where(item =>
                        item.Id == input.ConversationId.Value &&
                        item.UserId == input.UserId &&
                        item.Channel == CustomerConversationChannel.Support)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.Subject, item => item.Subject ?? normalizedSubject)
                        .SetProperty(item => item.Status, CustomerConversationStatus.Open)
                        .SetProperty(item => item.LastMessageAt,
                            item => item.LastMessageAt < now ? now : item.LastMessageAt)
                        .SetProperty(item => item.LastCustomerMessageAt,
                            item => !item.LastCustomerMessageAt.HasValue ||
                                item.LastCustomerMessageAt < now
                                    ? now
                                    : item.LastCustomerMessageAt)
                        .SetProperty(item => item.UpdatedAt, now)
                        .SetProperty(item => item.ClosedAt, (DateTime?)null), ct);
                if (updated == 0)
                {
                    await transaction.RollbackAsync(ct);
                    return CustomerMessageActionResult.NotFound();
                }

                conversation = new CustomerConversation { Id = input.ConversationId.Value };
                message = new CustomerMessage
                {
                    ConversationId = input.ConversationId.Value,
                    Sender = CustomerMessageSender.Customer,
                    UserId = input.UserId,
                    ClientMessageId = clientMessageId,
                    Body = body,
                    IsReadByAdmin = false,
                    CreatedAt = now,
                };
                db.CustomerMessages.Add(message);
            }
            else
            {
                conversation = new CustomerConversation
                {
                    UserId = input.UserId,
                    Subject = NormalizeSubject(input.Subject, body),
                    Channel = CustomerConversationChannel.Support,
                    Status = CustomerConversationStatus.Open,
                    LastMessageAt = now,
                    LastCustomerMessageAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                message = new CustomerMessage
                {
                    Conversation = conversation,
                    Sender = CustomerMessageSender.Customer,
                    UserId = input.UserId,
                    ClientMessageId = clientMessageId,
                    Body = body,
                    IsReadByAdmin = false,
                    CreatedAt = now,
                };
                db.CustomerMessages.Add(message);
            }

            try
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException exception) when (
                clientMessageId is not null &&
                IsUniqueConstraintViolation(exception))
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                return await TryResolveExistingCustomerMessageAsync(
                        input.ConversationId,
                        input.UserId,
                        clientMessageId,
                        body,
                        ct) ??
                    CustomerMessageActionResult.Failed(
                        new CustomerMessageValidationError(
                            nameof(input.ClientMessageId),
                            "Mã gửi tin đã được sử dụng."));
            }

            var conversationId = input.ConversationId ?? conversation.Id;
            return CustomerMessageActionResult.Success(
                "Đã ghi nhận tin nhắn khách hàng.",
                conversationId,
                message.Id);
        });

        await NotifySavedMessageAsync(result, ct);
        return result;
    }

    public async Task<CustomerMessageActionResult> RecordCustomerAiExchangeAsync(
        CustomerAiExchangeCreateModel input,
        CancellationToken ct = default)
    {
        var question = input.Question?.Trim() ?? string.Empty;
        var reply = input.Reply?.Trim() ?? string.Empty;
        var metadataJson = input.AiMetadataJson?.Trim() ?? "{}";
        var questionError = ValidateMessage(question, nameof(input.Question), "Nội dung khách hỏi AI");
        var replyError = ValidateMessage(reply, nameof(input.Reply), "Nội dung AI trả lời");
        var metadataError = ValidateMetadata(metadataJson);
        if (questionError is not null || replyError is not null || metadataError is not null)
        {
            return CustomerMessageActionResult.Failed(
                new[] { questionError, replyError, metadataError }
                    .Where(error => error is not null)
                    .Cast<CustomerMessageValidationError>()
                    .ToArray());
        }

        var receipt = receiptValidator.Validate(
            input.Receipt,
            input.UserId,
            question,
            reply,
            metadataJson);
        if (!receipt.Succeeded || string.IsNullOrWhiteSpace(receipt.ReceiptId))
        {
            return CustomerMessageActionResult.Failed(
                new CustomerMessageValidationError(
                    nameof(input.Receipt),
                    receipt.Error ?? "Chứng thực phản hồi AI không hợp lệ."));
        }

        var existingOutcome = await TryResolveExistingAiExchangeAsync(
            receipt.ReceiptId,
            input.UserId,
            question,
            reply,
            ct);
        if (existingOutcome is not null)
        {
            await NotifySavedMessagesAsync(existingOutcome, ct);
            return existingOutcome.Result;
        }

        var userExists = await db.Users.AsNoTracking().AnyAsync(user =>
            user.Id == input.UserId &&
            user.Role == UserRole.Customer &&
            user.IsActive, ct);
        if (!userExists)
        {
            return CustomerMessageActionResult.NotFound();
        }

        var now = DateTime.UtcNow;
        var executionStrategy = db.Database.CreateExecutionStrategy();
        var outcome = await executionStrategy.ExecuteAsync(async () =>
        {
            CustomerConversation conversation;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (input.ConversationId.HasValue)
            {
                var updated = await db.CustomerConversations
                    .Where(item =>
                        item.Id == input.ConversationId.Value &&
                        item.UserId == input.UserId &&
                        item.Channel == CustomerConversationChannel.Ai)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.Subject, item => item.Subject ?? "Trợ lý AI TechStore")
                        .SetProperty(item => item.Status, CustomerConversationStatus.AwaitingCustomer)
                        .SetProperty(item => item.LastMessageAt,
                            item => item.LastMessageAt < now ? now : item.LastMessageAt)
                        .SetProperty(item => item.LastCustomerMessageAt,
                            item => !item.LastCustomerMessageAt.HasValue ||
                                item.LastCustomerMessageAt < now
                                    ? now
                                    : item.LastCustomerMessageAt)
                        .SetProperty(item => item.LastAiMessageAt,
                            item => !item.LastAiMessageAt.HasValue ||
                                item.LastAiMessageAt < now
                                    ? now
                                    : item.LastAiMessageAt)
                        .SetProperty(item => item.UpdatedAt, now)
                        .SetProperty(item => item.ClosedAt, (DateTime?)null), ct);
                if (updated == 0)
                {
                    await transaction.RollbackAsync(ct);
                    return MessageSaveOutcome.FromResult(CustomerMessageActionResult.NotFound());
                }

                conversation = new CustomerConversation { Id = input.ConversationId.Value };
            }
            else
            {
                conversation = new CustomerConversation
                {
                    UserId = input.UserId,
                    Subject = "Trợ lý AI TechStore",
                    Channel = CustomerConversationChannel.Ai,
                    Status = CustomerConversationStatus.AwaitingCustomer,
                    LastMessageAt = now,
                    LastCustomerMessageAt = now,
                    LastAiMessageAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.CustomerConversations.Add(conversation);
            }

            var customerMessage = new CustomerMessage
            {
                ConversationId = input.ConversationId ?? 0,
                Conversation = input.ConversationId.HasValue ? null : conversation,
                Sender = CustomerMessageSender.Customer,
                UserId = input.UserId,
                Body = question,
                IsReadByAdmin = true,
                CreatedAt = now,
            };
            var aiMessage = new CustomerMessage
            {
                ConversationId = input.ConversationId ?? 0,
                Conversation = input.ConversationId.HasValue ? null : conversation,
                Sender = CustomerMessageSender.Ai,
                Body = reply,
                IsReadByAdmin = true,
                AiProvider = TrimNullable(receipt.AiProvider, 80),
                AiModel = TrimNullable(receipt.AiModel, 120),
                AiPrompt = TrimText(question, PromptMaxLength),
                AiResponseId = receipt.ReceiptId,
                AiMetadataJson = metadataJson,
                CreatedAt = now.AddTicks(1),
            };
            db.CustomerMessages.AddRange(customerMessage, aiMessage);

            try
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                return await TryResolveExistingAiExchangeAsync(
                        receipt.ReceiptId,
                        input.UserId,
                        question,
                        reply,
                        ct) ??
                    MessageSaveOutcome.FromResult(
                        CustomerMessageActionResult.Failed(
                            new CustomerMessageValidationError(
                                nameof(input.Receipt),
                                "Phản hồi AI này đã được lưu trước đó.")));
            }

            var conversationId = input.ConversationId ?? conversation.Id;
            return new MessageSaveOutcome(
                CustomerMessageActionResult.Success(
                    "Đã lưu trao đổi AI.",
                    conversationId,
                    aiMessage.Id),
                conversationId,
                [customerMessage.Id, aiMessage.Id]);
        });

        await NotifySavedMessagesAsync(outcome, ct);
        return outcome.Result;
    }

    public async Task<CustomerMessageActionResult> MarkConversationReadAsync(
        long conversationId,
        CancellationToken ct = default)
    {
        if (!await db.CustomerConversations.AsNoTracking()
                .AnyAsync(conversation => conversation.Id == conversationId, ct))
        {
            return CustomerMessageActionResult.NotFound();
        }

        var unreadCustomerMessages = await db.CustomerMessages
            .Where(message =>
                message.ConversationId == conversationId &&
                message.Sender == CustomerMessageSender.Customer &&
                !message.IsReadByAdmin)
            .ToListAsync(ct);
        foreach (var message in unreadCustomerMessages)
        {
            message.IsReadByAdmin = true;
        }

        if (unreadCustomerMessages.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            await realtimeNotifier.NotifyConversationChangedAsync(conversationId, ct);
        }

        return CustomerMessageActionResult.Success("Đã đánh dấu hội thoại là đã đọc.", conversationId);
    }

    private async Task NotifySavedMessageAsync(
        CustomerMessageActionResult result,
        CancellationToken ct)
    {
        if (result.Succeeded && result.ConversationId.HasValue && result.MessageId.HasValue)
        {
            await realtimeNotifier.NotifyMessageSavedAsync(
                result.ConversationId.Value,
                result.MessageId.Value,
                ct);
        }
    }

    private async Task NotifySavedMessagesAsync(
        MessageSaveOutcome outcome,
        CancellationToken ct)
    {
        if (outcome.Result.Succeeded &&
            outcome.ConversationId.HasValue &&
            outcome.MessageIds.Count > 0)
        {
            await realtimeNotifier.NotifyMessagesSavedAsync(
                outcome.ConversationId.Value,
                outcome.MessageIds,
                ct);
        }
    }

    private async Task<CustomerMessageActionResult?> TryResolveExistingCustomerMessageAsync(
        long? expectedConversationId,
        long userId,
        string? clientMessageId,
        string body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clientMessageId))
        {
            return null;
        }

        var existing = await db.CustomerMessages
            .AsNoTracking()
            .Where(message =>
                message.Sender == CustomerMessageSender.Customer &&
                message.UserId == userId &&
                message.ClientMessageId == clientMessageId)
            .Select(message => new ExistingMessageProjection(
                message.Id,
                message.ConversationId,
                message.Body))
            .FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            return null;
        }

        return BuildExistingMessageResult(
            existing,
            expectedConversationId,
            body,
            nameof(CustomerMessageCreateModel.ClientMessageId),
            "Tin nhắn này đã được ghi nhận trước đó.");
    }

    private async Task<CustomerMessageActionResult?> TryResolveExistingStaffMessageAsync(
        long conversationId,
        long staffId,
        string? clientMessageId,
        string body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clientMessageId))
        {
            return null;
        }

        var existing = await db.CustomerMessages
            .AsNoTracking()
            .Where(message =>
                message.Sender == CustomerMessageSender.Staff &&
                message.StaffId == staffId &&
                message.ClientMessageId == clientMessageId)
            .Select(message => new ExistingMessageProjection(
                message.Id,
                message.ConversationId,
                message.Body))
            .FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            return null;
        }

        return BuildExistingMessageResult(
            existing,
            conversationId,
            body,
            nameof(CustomerMessageReplyViewModel.ClientMessageId),
            "Phản hồi này đã được ghi nhận trước đó.");
    }

    private async Task<MessageSaveOutcome?> TryResolveExistingAiExchangeAsync(
        string receiptId,
        long userId,
        string question,
        string reply,
        CancellationToken ct)
    {
        var aiMessage = await db.CustomerMessages
            .AsNoTracking()
            .Where(message =>
                message.Sender == CustomerMessageSender.Ai &&
                message.AiResponseId == receiptId)
            .Select(message => new ExistingMessageProjection(
                message.Id,
                message.ConversationId,
                message.Body))
            .FirstOrDefaultAsync(ct);
        if (aiMessage is null)
        {
            return null;
        }

        if (!string.Equals(aiMessage.Body, reply, StringComparison.Ordinal))
        {
            return MessageSaveOutcome.FromResult(
                CustomerMessageActionResult.Failed(
                    new CustomerMessageValidationError(
                        nameof(CustomerAiExchangeCreateModel.Receipt),
                        "Chứng thực AI đã được dùng cho nội dung khác.")));
        }

        var customerMessageId = await db.CustomerMessages
            .AsNoTracking()
            .Where(message =>
                message.ConversationId == aiMessage.ConversationId &&
                message.Sender == CustomerMessageSender.Customer &&
                message.UserId == userId &&
                message.Body == question &&
                message.Id < aiMessage.Id)
            .OrderByDescending(message => message.Id)
            .Select(message => (long?)message.Id)
            .FirstOrDefaultAsync(ct);

        var messageIds = customerMessageId.HasValue
            ? new[] { customerMessageId.Value, aiMessage.Id }
            : [aiMessage.Id];
        return new MessageSaveOutcome(
            CustomerMessageActionResult.Success(
                "Trao đổi AI này đã được ghi nhận trước đó.",
                aiMessage.ConversationId,
                aiMessage.Id),
            aiMessage.ConversationId,
            messageIds);
    }

    private static CustomerMessageActionResult BuildExistingMessageResult(
        ExistingMessageProjection existing,
        long? expectedConversationId,
        string body,
        string fieldName,
        string successMessage)
    {
        if (expectedConversationId.HasValue && existing.ConversationId != expectedConversationId.Value)
        {
            return CustomerMessageActionResult.Failed(
                new CustomerMessageValidationError(fieldName, "Mã gửi tin đã thuộc hội thoại khác."));
        }

        if (!string.Equals(existing.Body, body, StringComparison.Ordinal))
        {
            return CustomerMessageActionResult.Failed(
                new CustomerMessageValidationError(fieldName, "Mã gửi tin đã được sử dụng cho nội dung khác."));
        }

        return CustomerMessageActionResult.Success(successMessage, existing.ConversationId, existing.Id);
    }

    private static CustomerMessageValidationError? ValidateMessage(
        string value,
        string fieldName,
        string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new CustomerMessageValidationError(fieldName, $"{label} là bắt buộc.");
        }

        return value.Length > ReplyMaxLength
            ? new CustomerMessageValidationError(fieldName, $"{label} tối đa {ReplyMaxLength} ký tự.")
            : null;
    }

    private static CustomerMessageValidationError? ValidateClientMessageId(
        string? value,
        string fieldName,
        bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return required
                ? new CustomerMessageValidationError(fieldName, "Thiếu mã gửi tin an toàn.")
                : null;
        }

        return value.Length > ClientMessageIdMaxLength
            ? new CustomerMessageValidationError(fieldName, $"Mã gửi tin tối đa {ClientMessageIdMaxLength} ký tự.")
            : null;
    }

    private static CustomerMessageValidationError? ValidateMetadata(string metadataJson)
    {
        if (metadataJson.Length > MetadataMaxLength)
        {
            return new CustomerMessageValidationError(
                nameof(CustomerAiExchangeCreateModel.AiMetadataJson),
                $"Metadata AI tối đa {MetadataMaxLength} ký tự.");
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? null
                : new CustomerMessageValidationError(
                    nameof(CustomerAiExchangeCreateModel.AiMetadataJson),
                    "Metadata AI phải là một JSON object.");
        }
        catch (JsonException)
        {
            return new CustomerMessageValidationError(
                nameof(CustomerAiExchangeCreateModel.AiMetadataJson),
                "Metadata AI không phải JSON hợp lệ.");
        }
    }

    private static string? NormalizeClientMessageId(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private sealed record ExistingMessageProjection(long Id, long ConversationId, string Body);

    private sealed record MessageSaveOutcome(
        CustomerMessageActionResult Result,
        long? ConversationId = null,
        IReadOnlyCollection<long>? SavedMessageIds = null)
    {
        public IReadOnlyCollection<long> MessageIds => SavedMessageIds ?? [];

        public static MessageSaveOutcome FromResult(CustomerMessageActionResult result) => new(result);
    }
}
