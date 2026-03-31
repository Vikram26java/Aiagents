using BusinessAiAgent.Core.Entities;

namespace BusinessAiAgent.Core.Interfaces;

public interface IDirectMessageService
{
    Task<Conversation> GetOrCreateDirectConversationAsync(string userId1, string userId2);
    Task<Message> SendDirectMessageAsync(int conversationId, string senderId, string body);
    Task<List<Conversation>> GetDirectConversationsAsync(string userId);
}
