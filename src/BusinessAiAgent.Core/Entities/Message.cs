using BusinessAiAgent.Core.Enums;

namespace BusinessAiAgent.Core.Entities;

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public MessageDirection Direction { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? SenderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
}
