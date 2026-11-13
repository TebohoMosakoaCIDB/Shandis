using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Invitations;

public sealed class CreateInvitationCommand
{
    public Guid MemoryId { get; init; }
    public Guid InvitedByUserId { get; init; }
    public string InvitedEmail { get; init; } = string.Empty;
    public InvitationRole AssignedRole { get; init; }
}
