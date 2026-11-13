
namespace Shandis.Application.Features.Invitations;

public sealed class ReceivedInvitationsResult
{
    public IReadOnlyList<InvitationItemResult> Items { get; init; }
        = Array.Empty<InvitationItemResult>();
}
