using System.ComponentModel.DataAnnotations;
using Shandis.Domain.Enums;

namespace Shandis.Api.Contracts.Invitations;

public class CreateInvitationRequest
{
    [Required]
    [MaxLength(320)]
    public string Invitee { get; set; } = string.Empty;

    public InvitationRole AssignedRole { get; set; }
}