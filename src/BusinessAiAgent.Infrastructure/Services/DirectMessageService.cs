using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessAiAgent.Infrastructure.Services;

public class DirectMessageService : IDirectMessageService
{
    private readonly AppDbContext _db;

    public DirectMessageService(AppDbContext db) => _db = db;

    public async Task<Conversation> GetOrCreateDirectConversationAsync(string userId1, string userId2)
    {
        // Create a deterministic key for the pair
        var ids = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
        var subject = $"DM:{ids[0]}:{ids[1]}";

        var conversation = await _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Channel == ChannelType.Direct && c.Subject == subject);

        if (conversation != null)
            return conversation;

        conversation = new Conversation
        {
            Channel = ChannelType.Direct,
            Subject = subject
        };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();
        return conversation;
    }

    public async Task<Message> SendDirectMessageAsync(int conversationId, string senderId, string body)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            Direction = MessageDirection.Inbound,
            Body = body,
            SenderId = senderId
        };
        _db.Messages.Add(message);

        var conversation = await _db.Conversations.FindAsync(conversationId);
        if (conversation != null)
            conversation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return message;
    }

    public async Task<List<Conversation>> GetDirectConversationsAsync(string userId)
    {
        return await _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .Where(c => c.Channel == ChannelType.Direct && c.Subject.Contains(userId))
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync();
    }
}
