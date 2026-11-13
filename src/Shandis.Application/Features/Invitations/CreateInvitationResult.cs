using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Invitations;

public sealed class CreateInvitationResult
{
    public Guid Id { get; init; }
    public Guid MemoryId { get; init; }
    public string InvitedEmail { get; init; } = string.Empty;
    public InvitationRole AssignedRole { get; init; }
    public InvitationStatus Status { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
