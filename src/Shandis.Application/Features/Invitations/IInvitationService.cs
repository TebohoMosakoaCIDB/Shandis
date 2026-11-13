
namespace Shandis.Application.Features.Invitations;

public interface IInvitationService
{
    Task<CreateInvitationResult> CreateAsync(
        CreateInvitationCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvitationItemResult>> GetForMemoryAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    Task<ReceivedInvitationsResult> GetReceivedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AcceptAsync(
    Guid invitationId,
    Guid userId,
    string userEmail,
    CancellationToken cancellationToken = default);

    Task DeclineAsync(
        Guid invitationId,
        Guid userId,
        string userEmail,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid invitationId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);
}
