namespace Shandis.Application.Abstractions.Email;

public interface IEmailService
{
    Task SendMemoryInvitationAsync(
        string recipientEmail,
        string memoryTitle,
        string invitationToken,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}
