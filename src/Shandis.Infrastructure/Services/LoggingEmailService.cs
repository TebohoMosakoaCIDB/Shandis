
using Microsoft.Extensions.Logging;
using Shandis.Application.Abstractions.Email;

namespace Shandis.Infrastructure.Services;

public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendMemoryInvitationAsync(
        string recipientEmail,
        string memoryTitle,
        string invitationToken,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            DEVELOPMENT MEMORY INVITATION
            Recipient: {RecipientEmail}
            Memory: {MemoryTitle}
            Token: {InvitationToken}
            Expires: {ExpiresAtUtc}
            """,
            recipientEmail,
            memoryTitle,
            invitationToken,
            expiresAtUtc);

        return Task.CompletedTask;
    }
}
