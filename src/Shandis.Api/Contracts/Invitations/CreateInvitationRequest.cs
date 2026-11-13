using Shandis.Domain.Enums;

namespace Shandis.Api.Contracts.Invitations;

public class CreateInvitationRequest
{
    public string InvitedEmail { get; set; } = string.Empty;

    public InvitationRole AssignedRole { get; set; }
}
