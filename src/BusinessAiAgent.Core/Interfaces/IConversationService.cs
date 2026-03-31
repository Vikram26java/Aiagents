using BusinessAiAgent.Core.DTOs;
using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;

namespace BusinessAiAgent.Core.Interfaces;

public interface IConversationService
{
    Task<Conversation> CreateConversationAsync(ChannelType channel, string subject, int? contactId = null);
    Task<Conversation?> GetConversationAsync(int id);
    Task<List<Conversation>> GetConversationsAsync(ChannelType? channelFilter = null);
    Task<List<Conversation>> GetConversationsForContactAsync(int contactId);
    Task<Message> AddMessageAsync(int conversationId, MessageDirection direction, string body);
    Task<Conversation> UpdateConversationAsync(int id, string subject);
    Task DeleteConversationAsync(int id);
    Task<DashboardStats> GetDashboardStatsAsync();
}
