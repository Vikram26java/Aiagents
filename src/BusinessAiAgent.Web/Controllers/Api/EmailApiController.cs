using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BusinessAiAgent.Web.Controllers.Api;

[ApiController]
[Route("api/email")]
public class EmailApiController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly SendGridSettings _sendGridSettings;

    public EmailApiController(IEmailService emailService, IOptions<SendGridSettings> sendGridSettings)
    {
        _emailService = emailService;
        _sendGridSettings = sendGridSettings.Value;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
    {
        await _emailService.SendEmailAsync(
            request.To, request.From ?? _sendGridSettings.FromEmail, request.Subject, request.Body);
        return Ok(new { status = "sent" });
    }

    [HttpPost("inbound")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> InboundEmail()
    {
        var form = await Request.ReadFormAsync();
        var from = form["from"].ToString();
        var to = form["to"].ToString();
        var subject = form["subject"].ToString();
        var body = form["text"].ToString();

        // Extract email address from "Name <email>" format
        var fromEmail = from;
        if (from.Contains('<') && from.Contains('>'))
        {
            fromEmail = from[(from.IndexOf('<') + 1)..from.IndexOf('>')];
        }

        await _emailService.ProcessInboundEmailAsync(fromEmail, to, subject, body);
        return Ok();
    }
}

public record SendEmailRequest(string To, string? From, string Subject, string Body);
