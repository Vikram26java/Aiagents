namespace BusinessAiAgent.Core.Interfaces;

public interface ISmsService
{
    Task<string> SendSmsAsync(string to, string from, string body, int? conversationId = null);
    Task<string> ProcessInboundSmsAsync(string from, string to, string body, string messageSid);
}
