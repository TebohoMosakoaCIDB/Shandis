using Shandis.Domain.Enums;

namespace Shandis.Api.Contracts.Invitations;

public class InvitationItemResponse
{
    public Guid Id { get; set; }
    public Guid MemoryId { get; set; }
    public string MemoryTitle { get; set; } = string.Empty;
    public Guid InvitedByUserId { get; set; }
    public Guid? InvitedUserId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public InvitationRole AssignedRole { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
