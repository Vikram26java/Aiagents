using BusinessAiAgent.Core.DTOs;
using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessAiAgent.Infrastructure.Services;

public class ConversationService : IConversationService
{
    private readonly AppDbContext _db;

    public ConversationService(AppDbContext db) => _db = db;

    public async Task<Conversation> CreateConversationAsync(ChannelType channel, string subject, int? contactId = null)
    {
        var conversation = new Conversation
        {
            Channel = channel,
            Subject = subject,
            ContactId = contactId
        };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();
        return conversation;
    }

    public async Task<Conversation?> GetConversationAsync(int id)
    {
        return await _db.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .Include(c => c.Contact)
            .Include(c => c.CallLogs)
            .Include(c => c.SmsRecords)
            .Include(c => c.EmailRecords)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Conversation>> GetConversationsAsync(ChannelType? channelFilter = null)
    {
        var query = _db.Conversations
            .Include(c => c.Contact)
            .Include(c => c.Messages)
            .AsQueryable();

        if (channelFilter.HasValue)
            query = query.Where(c => c.Channel == channelFilter.Value);

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<List<Conversation>> GetConversationsForContactAsync(int contactId)
    {
        return await _db.Conversations
            .Include(c => c.Messages)
            .Where(c => c.ContactId == contactId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Message> AddMessageAsync(int conversationId, MessageDirection direction, string body)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            Direction = direction,
            Body = body
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        return message;
    }

    public async Task<Conversation> UpdateConversationAsync(int id, string subject)
    {
        var conversation = await _db.Conversations.FindAsync(id)
            ?? throw new KeyNotFoundException($"Conversation {id} not found");

        conversation.Subject = subject;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return conversation;
    }

    public async Task DeleteConversationAsync(int id)
    {
        var conversation = await _db.Conversations.FindAsync(id)
            ?? throw new KeyNotFoundException($"Conversation {id} not found");

        _db.Conversations.Remove(conversation);
        await _db.SaveChangesAsync();
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        return new DashboardStats
        {
            TotalConversations = await _db.Conversations.CountAsync(),
            TotalMessages = await _db.Messages.CountAsync(),
            TotalContacts = await _db.Contacts.CountAsync(),
            ChatCount = await _db.Conversations.CountAsync(c => c.Channel == ChannelType.Chat),
            VoiceCount = await _db.Conversations.CountAsync(c => c.Channel == ChannelType.Voice),
            SmsCount = await _db.Conversations.CountAsync(c => c.Channel == ChannelType.Sms),
            EmailCount = await _db.Conversations.CountAsync(c => c.Channel == ChannelType.Email)
        };
    }
}
