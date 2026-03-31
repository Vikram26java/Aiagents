namespace BusinessAiAgent.Core.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string from, string subject, string body, int? conversationId = null);
    Task<string> ProcessInboundEmailAsync(string from, string to, string subject, string body);
}
