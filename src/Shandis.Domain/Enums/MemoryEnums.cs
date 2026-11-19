
namespace Shandis.Domain.Enums;

public enum MemoryVisibility
{
    Public = 1,
    Private = 2
}

public enum MemoryContributionPolicy
{
    Open = 1,
    ApprovalRequired = 2,
    InvitationOnly = 3
}

public enum MemoryStatus
{
    Draft = 1,
    Published = 2,
    Completed = 3,
    Archived = 4
}

public enum MomentType
{
    Photo = 1,
    Video = 2,
    Audio = 3,
    Story = 4,
    Mixed = 5
}

public enum MomentSourceType
{
    Owner = 1,
    Community = 2
}

public enum MomentStatus
{
    Draft = 1,
    Pending = 2,
    Published = 3,
    Rejected = 4,
    Removed = 5
}

public enum ParticipantRole
{
    Owner = 1,
    Contributor = 2,
    Viewer = 3
}

public enum ParticipantStatus
{
    Active = 1,
    Removed = 2
}

public enum ParticipantJoinSource
{
    Owner = 1,
    Invitation = 2,
    Contribution = 3
}

public enum InvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Declined = 3,
    Revoked = 4,
    Expired = 5
}

public enum InvitationRole
{
    Viewer = 1,
    Contributor = 2
}

public enum ReactionType
{
    Like = 1,
    Love = 2,
    Laugh = 3,
    Wow = 4
}

public enum NotificationType
{
    MemoryInvitationReceived = 1,
    InvitationAccepted = 2,
    InvitationDeclined = 3,
    MomentPendingApproval = 4,
    MomentApproved = 5,
    MomentRejected = 6,
    MomentLiked = 7,
    MomentCommented = 8,
    MomentPublished = 9
}
