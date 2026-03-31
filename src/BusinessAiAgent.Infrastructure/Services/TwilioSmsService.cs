using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Enums;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Data;
using BusinessAiAgent.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace BusinessAiAgent.Infrastructure.Services;

public class TwilioSmsService : ISmsService
{
    private readonly TwilioSettings _settings;
    private readonly AppDbContext _db;
    private readonly IChatService _chatService;
    private readonly IContactService _contactService;

    public TwilioSmsService(IOptions<TwilioSettings> settings, AppDbContext db, IChatService chatService, IContactService contactService)
    {
        _settings = settings.Value;
        _db = db;
        _chatService = chatService;
        _contactService = contactService;
        TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
    }

    public async Task<string> SendSmsAsync(string to, string from, string body, int? conversationId = null)
    {
        if (conversationId == null)
        {
            var contact = await _contactService.GetOrCreateContactAsync(phone: to);
            var conversation = new Conversation { Channel = ChannelType.Sms, Subject = $"SMS to {to}", ContactId = contact.Id };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();
            conversationId = conversation.Id;
        }

        var message = await MessageResource.CreateAsync(
            to: new PhoneNumber(to),
            from: new PhoneNumber(from),
            body: body);

        _db.Messages.Add(new Message
        {
            ConversationId = conversationId.Value,
            Direction = MessageDirection.Outbound,
            Body = body
        });

        _db.SmsRecords.Add(new SmsRecord
        {
            ConversationId = conversationId.Value,
            MessageSid = message.Sid,
            From = from,
            To = to,
            Status = message.Status.ToString()
        });

        await _db.SaveChangesAsync();
        return message.Sid;
    }

    public async Task<string> ProcessInboundSmsAsync(string from, string to, string body, string messageSid)
    {
        var contact = await _contactService.GetOrCreateContactAsync(phone: from);
        var conversation = new Conversation { Channel = ChannelType.Sms, Subject = $"SMS from {from}", ContactId = contact.Id };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        // Store inbound message
        var inboundMsg = new Message { ConversationId = conversation.Id, Direction = MessageDirection.Inbound, Body = body };
        _db.Messages.Add(inboundMsg);
        _db.SmsRecords.Add(new SmsRecord
        {
            ConversationId = conversation.Id,
            MessageSid = messageSid,
            From = from,
            To = to,
            Status = "received"
        });
        await _db.SaveChangesAsync();

        // Get AI reply
        var aiReply = await _chatService.GetAiResponseAsync(new[] { inboundMsg }, body);

        // Store outbound AI reply
        _db.Messages.Add(new Message { ConversationId = conversation.Id, Direction = MessageDirection.Outbound, Body = aiReply });
        await _db.SaveChangesAsync();

        return aiReply;
    }
}
