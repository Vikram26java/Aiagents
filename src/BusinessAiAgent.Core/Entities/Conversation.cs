using BusinessAiAgent.Core.Enums;

namespace BusinessAiAgent.Core.Entities;

public class Conversation
{
    public int Id { get; set; }
    public int? ContactId { get; set; }
    public ChannelType Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Contact? Contact { get; set; }
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<CallLog> CallLogs { get; set; } = [];
    public ICollection<SmsRecord> SmsRecords { get; set; } = [];
    public ICollection<EmailRecord> EmailRecords { get; set; } = [];
}
