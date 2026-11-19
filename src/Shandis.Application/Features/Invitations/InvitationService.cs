
using System.Security.Cryptography;
using System.Text;
using Shandis.Application.Abstractions.Email;
using Shandis.Application.Abstractions.Persistence;\nusing Shandis.Application.Features.Notifications;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Invitations;

public class InvitationService : IInvitationService
{
    private static readonly TimeSpan InvitationLifetime =
        TimeSpan.FromDays(7);

    private readonly IInvitationRepository _invitationRepository;
    private readonly IEmailService _emailService;\n    private readonly INotificationService _notificationService;

    public InvitationService(
        IInvitationRepository invitationRepository,
        IEmailService emailService)
    {
        _invitationRepository = invitationRepository;
        _emailService = emailService;\n        _notificationService = notificationService;
    }

    public async Task<CreateInvitationResult> CreateAsync(
        CreateInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.MemoryId == Guid.Empty)
            throw new ArgumentException("A valid Memory ID is required.");

        if (command.InvitedByUserId == Guid.Empty)
            throw new ArgumentException("A valid inviting user ID is required.");

        if (string.IsNullOrWhiteSpace(command.InvitedEmail))
            throw new ArgumentException("An email address is required.");

        if (command.AssignedRole is not InvitationRole.Viewer
            and not InvitationRole.Contributor)
        {
            throw new ArgumentException(
                "Only Viewer and Contributor invitation roles are supported.");
        }

        var email = command.InvitedEmail.Trim().ToLowerInvariant();

        var memoryOwnerId =
            await _invitationRepository.GetMemoryOwnerIdAsync(
                command.MemoryId,
                cancellationToken);

        if (memoryOwnerId is null)
            throw new InvalidOperationException("The Memory does not exist.");

        if (memoryOwnerId.Value != command.InvitedByUserId)
            throw new UnauthorizedAccessException(
                "Only the Memory owner can invite participants.");

        var ownerEmail = await _invitationRepository.GetUserEmailAsync(
            command.InvitedByUserId,
            cancellationToken);

        if (string.Equals(ownerEmail, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "You cannot invite yourself to your own Memory.");

        var existingUserId =
            await _invitationRepository.FindUserIdByEmailAsync(
                email,
                cancellationToken);

        if (existingUserId == command.InvitedByUserId)
            throw new InvalidOperationException(
                "You cannot invite yourself to your own Memory.");

        var hasPendingInvitation =
            await _invitationRepository.HasPendingInvitationAsync(
                command.MemoryId,
                email,
                cancellationToken);

        if (hasPendingInvitation)
            throw new InvalidOperationException(
                "A pending invitation already exists for this email address.");

        var memoryTitle = await _invitationRepository.GetMemoryTitleAsync(command.MemoryId, cancellationToken);

        if (memoryTitle is null)
            throw new InvalidOperationException("The Memory does not exist.");

        var rawToken = GenerateToken();
        var tokenHash = HashToken(rawToken);
        var expiresAtUtc = DateTime.UtcNow.Add(InvitationLifetime);

        var invitation = new MemoryInvitation(
            memoryId: command.MemoryId,
            invitedByUserId: command.InvitedByUserId,
            invitedEmail: email,
            assignedRole: command.AssignedRole,
            tokenHash: tokenHash,
            expiresAtUtc: expiresAtUtc,
            invitedUserId: existingUserId);

        await _invitationRepository.AddAsync(
            invitation,
            cancellationToken);

        await _emailService.SendMemoryInvitationAsync(
            email,
            memoryTitle,
            rawToken,
            expiresAtUtc,
            cancellationToken);

        return new CreateInvitationResult
        {
            Id = invitation.Id,
            MemoryId = invitation.MemoryId,
            InvitedEmail = invitation.InvitedEmail,
            AssignedRole = invitation.AssignedRole,
            Status = invitation.Status,
            ExpiresAtUtc = invitation.ExpiresAtUtc,
            CreatedAtUtc = invitation.CreatedAtUtc
        };
    }

    public async Task<IReadOnlyList<InvitationItemResult>> GetForMemoryAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        if (memoryId == Guid.Empty || requestingUserId == Guid.Empty)
            throw new ArgumentException("Valid Memory and user IDs are required.");

        var memoryOwnerId =
            await _invitationRepository.GetMemoryOwnerIdAsync(
                memoryId,
                cancellationToken);

        if (memoryOwnerId is null)
            throw new InvalidOperationException("The Memory does not exist.");

        if (memoryOwnerId.Value != requestingUserId)
            throw new UnauthorizedAccessException(
                "Only the Memory owner can view its invitations.");

        await _invitationRepository.ExpirePendingInvitationsAsync(
            memoryId: memoryId,
            cancellationToken: cancellationToken);

        var invitations = await _invitationRepository.GetForMemoryAsync(
            memoryId,
            cancellationToken);

        return invitations.Select(MapInvitation).ToList();
    }

    public async Task<ReceivedInvitationsResult> GetReceivedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.");

        var email = await _invitationRepository.GetUserEmailAsync(
            userId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException(
                "The authenticated user's email address could not be found.");

        await _invitationRepository.ExpirePendingInvitationsAsync(
            userId: userId,
            cancellationToken: cancellationToken);

        var invitations = await _invitationRepository.GetReceivedAsync(
            userId,
            email,
            cancellationToken);

        return new ReceivedInvitationsResult
        {
            Items = invitations.Select(MapInvitation).ToList()
        };
    }

    private static InvitationItemResult MapInvitation(
        MemoryInvitation invitation)
    {
        return new InvitationItemResult
        {
            Id = invitation.Id,
            MemoryId = invitation.MemoryId,
            MemoryTitle = invitation.Memory?.Title ?? string.Empty,
            InvitedByUserId = invitation.InvitedByUserId,
            InvitedUserId = invitation.InvitedUserId,
            InvitedEmail = invitation.InvitedEmail,
            AssignedRole = invitation.AssignedRole,
            Status = invitation.Status,
            ExpiresAtUtc = invitation.ExpiresAtUtc,
            CreatedAtUtc = invitation.CreatedAtUtc
        };
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(hash);
    }

    public async Task AcceptAsync(
    Guid invitationId,
    Guid userId,
    string userEmail,
    CancellationToken cancellationToken = default)
    {
        if (invitationId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Valid invitation and user IDs are required.");

        if (string.IsNullOrWhiteSpace(userEmail))
            throw new ArgumentException("A valid email address is required.");

        var invitation = await _invitationRepository.GetByIdAsync(
            invitationId,
            cancellationToken);

        if (invitation is null)
            throw new InvalidOperationException("Invitation not found.");

        if (!string.Equals(
            invitation.InvitedEmail,
            userEmail.Trim(),
            StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "This invitation was issued to another email address.");
        }

        if (invitation.Status != InvitationStatus.Pending)
            throw new InvalidOperationException("This invitation is no longer pending.");

        if (invitation.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("This invitation has expired.");
        }


        var participantRole = invitation.AssignedRole switch
        {
            InvitationRole.Viewer => ParticipantRole.Viewer,
            InvitationRole.Contributor => ParticipantRole.Contributor,
            _ => throw new InvalidOperationException(
                "The invitation contains an unsupported participant role.")
        };

        var existingParticipant =
            await _invitationRepository.GetParticipantAsync(
                invitation.MemoryId,
                userId,
                cancellationToken);

        var isExistingParticipant = existingParticipant is not null;

        if (existingParticipant is not null)
        {
            if (existingParticipant.Status == ParticipantStatus.Active)
            {
                throw new InvalidOperationException(
                    "You are already a participant in this Memory.");
            }

            existingParticipant.Reactivate(
                participantRole,
                ParticipantJoinSource.Invitation);
        }
        else
        {
            existingParticipant = new MemoryParticipant(
                invitation.MemoryId,
                userId,
                participantRole,
                ParticipantJoinSource.Invitation);
        }

        invitation.Accept(userId);

        await _invitationRepository.AcceptAsync(
            invitation,
            existingParticipant,
            isExistingParticipant,
            cancellationToken);

    }

    public async Task DeclineAsync(
    Guid invitationId,
    Guid userId,
    string userEmail,
    CancellationToken cancellationToken = default)
    {
        if (invitationId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Valid invitation and user IDs are required.");

        if (string.IsNullOrWhiteSpace(userEmail))
            throw new ArgumentException("A valid email address is required.");

        var invitation = await _invitationRepository.GetByIdAsync(
            invitationId,
            cancellationToken);

        if (invitation is null)
            throw new InvalidOperationException("Invitation not found.");

        if (!string.Equals(
            invitation.InvitedEmail,
            userEmail.Trim(),
            StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "This invitation was issued to another email address.");
        }

        if (invitation.Status != InvitationStatus.Pending)
            throw new InvalidOperationException("This invitation is no longer pending.");

        invitation.Decline();

        await _invitationRepository.UpdateAsync(
            invitation,
            cancellationToken);
    }

    public async Task RevokeAsync(
    Guid invitationId,
    Guid requestingUserId,
    CancellationToken cancellationToken = default)
    {
        if (invitationId == Guid.Empty || requestingUserId == Guid.Empty)
            throw new ArgumentException("Valid invitation and user IDs are required.");

        var invitation = await _invitationRepository.GetByIdAsync(
            invitationId,
            cancellationToken);

        if (invitation is null)
            throw new InvalidOperationException("Invitation not found.");

        if (invitation.InvitedByUserId != requestingUserId)
            throw new UnauthorizedAccessException(
                "Only the user who created this invitation can revoke it.");

        invitation.Revoke();

        await _invitationRepository.UpdateAsync(
            invitation,
            cancellationToken);
    }
}
