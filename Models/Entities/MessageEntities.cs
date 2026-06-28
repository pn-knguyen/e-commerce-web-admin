using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Models.Entities;

public class CustomerConversation
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? AssignedStaffId { get; set; }
    public string? Subject { get; set; }
    public CustomerConversationChannel Channel { get; set; } = CustomerConversationChannel.Support;
    public CustomerConversationStatus Status { get; set; } = CustomerConversationStatus.Open;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastCustomerMessageAt { get; set; }
    public DateTime? LastStaffMessageAt { get; set; }
    public DateTime? LastAiMessageAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public User? User { get; set; }
    public Staff? AssignedStaff { get; set; }
    public ICollection<CustomerMessage> Messages { get; set; } = new List<CustomerMessage>();
}

public class CustomerMessage
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public CustomerMessageSender Sender { get; set; }
    public long? UserId { get; set; }
    public long? StaffId { get; set; }
    public string? ClientMessageId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsReadByAdmin { get; set; }
    public string? AiProvider { get; set; }
    public string? AiModel { get; set; }
    public string? AiPrompt { get; set; }
    public string? AiResponseId { get; set; }
    public string? AiMetadataJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CustomerConversation? Conversation { get; set; }
    public User? User { get; set; }
    public Staff? Staff { get; set; }
}
