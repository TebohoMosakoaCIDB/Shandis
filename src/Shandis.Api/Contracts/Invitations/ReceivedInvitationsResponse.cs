namespace Shandis.Api.Contracts.Invitations;

public class ReceivedInvitationsResponse
{
    public IReadOnlyList<InvitationItemResponse> Items { get; set; }
        = Array.Empty<InvitationItemResponse>();
}
