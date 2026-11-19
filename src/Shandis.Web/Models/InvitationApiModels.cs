namespace Shandis.Web.Models;

public sealed class CreateInvitationRequest
{
    public string Invitee { get; set; } = string.Empty;
    public int AssignedRole { get; set; } = 2;
}

public sealed class InvitationItem
{
    public Guid Id { get; set; }
    public Guid MemoryId { get; set; }
    public string MemoryTitle { get; set; } = string.Empty;
    public Guid InvitedByUserId { get; set; }
    public Guid? InvitedUserId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public int AssignedRole { get; set; }
    public int Status { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ReceivedInvitationsResponse
{
    public List<InvitationItem> Items { get; set; } = [];
}
