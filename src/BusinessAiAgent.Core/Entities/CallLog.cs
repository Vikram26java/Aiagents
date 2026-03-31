namespace BusinessAiAgent.Core.Entities;

public class CallLog
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string CallSid { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? Status { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
}
