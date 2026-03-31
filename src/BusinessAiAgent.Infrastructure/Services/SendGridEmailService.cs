using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Data;
using BusinessAiAgent.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BusinessAiAgent.Infrastructure.Services;

public class SendGridEmailService : IEmailService
{
    private readonly SendGridSettings _settings;
    private readonly AppDbContext _db;
    private readonly IChatService _chatService;
    private readonly IContactService _contactService;

    public SendGridEmailService(IOptions<SendGridSettings> settings, AppDbContext db, IChatService chatService, IContactService contactService)
    {
        _settings = settings.Value;
        _db = db;
        _chatService = chatService;
        _contactService = contactService;
    }

    public async Task SendEmailAsync(string to, string from, string subject, string body, int? conversationId = null)
    {
        if (conversationId == null)
        {
            var contact = await _contactService.GetOrCreateContactAsync(email: to);
            var conversation = new Conversation { Channel = ChannelType.Email, Subject = subject, ContactId = contact.Id };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();
            conversationId = conversation.Id;
        }

        var client = new SendGridClient(_settings.ApiKey);
        var msg = new SendGridMessage
        {
            From = new EmailAddress(from, _settings.FromName),
            Subject = subject,
            PlainTextContent = body
        };
        msg.AddTo(new EmailAddress(to));

        var response = await client.SendEmailAsync(msg);

        _db.Messages.Add(new Message
        {
            ConversationId = conversationId.Value,
            Direction = MessageDirection.Outbound,
            Body = body
        });

        _db.EmailRecords.Add(new EmailRecord
        {
            ConversationId = conversationId.Value,
            SendGridMessageId = response.Headers?.GetValues("X-Message-Id")?.FirstOrDefault() ?? "",
            FromEmail = from,
            ToEmail = to,
            SubjectLine = subject,
            Status = response.StatusCode.ToString()
        });

        await _db.SaveChangesAsync();
    }

    public async Task<string> ProcessInboundEmailAsync(string from, string to, string subject, string body)
    {
        var contact = await _contactService.GetOrCreateContactAsync(email: from);
        var conversation = new Conversation { Channel = ChannelType.Email, Subject = subject, ContactId = contact.Id };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        var inboundMsg = new Message { ConversationId = conversation.Id, Direction = MessageDirection.Inbound, Body = body };
        _db.Messages.Add(inboundMsg);
        _db.EmailRecords.Add(new EmailRecord
        {
            ConversationId = conversation.Id,
            SendGridMessageId = "",
            FromEmail = from,
            ToEmail = to,
            SubjectLine = subject,
            Status = "received"
        });
        await _db.SaveChangesAsync();

        // AI auto-reply
        var aiReply = await _chatService.GetAiResponseAsync(new[] { inboundMsg }, body);

        _db.Messages.Add(new Message { ConversationId = conversation.Id, Direction = MessageDirection.Outbound, Body = aiReply });
        await _db.SaveChangesAsync();

        // Send the reply email
        await SendEmailAsync(from, _settings.FromEmail, $"Re: {subject}", aiReply, conversation.Id);

        return aiReply;
    }
}
