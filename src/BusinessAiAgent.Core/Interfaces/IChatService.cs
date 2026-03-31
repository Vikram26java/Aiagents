using BusinessAiAgent.Core.Entities;

namespace BusinessAiAgent.Core.Interfaces;

public interface IChatService
{
    Task<string> GetAiResponseAsync(IEnumerable<Message> conversationHistory, string userMessage);
}
