namespace BusinessAiAgent.Core.Entities;

public class EmailRecord
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string SendGridMessageId { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string? SubjectLine { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
}
