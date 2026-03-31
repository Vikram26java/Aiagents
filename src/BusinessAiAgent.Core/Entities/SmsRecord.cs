namespace BusinessAiAgent.Core.Entities;

public class SmsRecord
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string MessageSid { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
}
