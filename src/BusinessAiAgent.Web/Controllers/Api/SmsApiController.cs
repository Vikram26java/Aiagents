using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BusinessAiAgent.Web.Controllers.Api;

[ApiController]
[Route("api/sms")]
public class SmsApiController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly TwilioSettings _twilioSettings;

    public SmsApiController(ISmsService smsService, IOptions<TwilioSettings> twilioSettings)
    {
        _smsService = smsService;
        _twilioSettings = twilioSettings.Value;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsRequest request)
    {
        var sid = await _smsService.SendSmsAsync(
            request.To, request.From ?? _twilioSettings.PhoneNumber, request.Body);
        return Ok(new { messageSid = sid });
    }

    [HttpPost("webhook")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> InboundSms([FromForm] IFormCollection form)
    {
        var from = form["From"].ToString();
        var to = form["To"].ToString();
        var body = form["Body"].ToString();
        var messageSid = form["MessageSid"].ToString();

        var aiReply = await _smsService.ProcessInboundSmsAsync(from, to, body, messageSid);

        // Return TwiML response
        var twiml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <Response>
                <Message>{System.Security.SecurityElement.Escape(aiReply)}</Message>
            </Response>
            """;

        return Content(twiml, "application/xml");
    }
}

public record SendSmsRequest(string To, string? From, string Body);
