
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Shandis.Application.Abstractions.Email;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Application.Features.Notifications;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Invitations;

public class InvitationService : IInvitationService
{
    private static readonly TimeSpan InvitationLifetime =
        TimeSpan.FromDays(7);

    private readonly IInvitationRepository _invitationRepository;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public InvitationService(
        IInvitationRepository invitationRepository,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _invitationRepository = invitationRepository;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task<CreateInvitationResult> CreateAsync(
        CreateInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.MemoryId == Guid.Empty)
            throw new ArgumentException("A valid Memory ID is required.");

        if (command.InvitedByUserId == Guid.Empty)
            throw new ArgumentException("A valid inviting user ID is required.");

        if (string.IsNullOrWhiteSpace(command.Invitee))
            throw new ArgumentException("Enter a Shandis username or email address.");

        if (command.AssignedRole is not InvitationRole.Viewer
            and not InvitationRole.Contributor)
        {
            throw new ArgumentException(
                "Only Viewer and Contributor invitation roles are supported.");
        }

        var invitee = command.Invitee.Trim();
        string email;
        Guid? existingUserId;

        if (invitee.StartsWith("@", StringComparison.Ordinal))
        {
            var userName = invitee[1..].Trim().ToLowerInvariant();

            if (userName.Length is < 3 or > 30 ||
                userName.Any(character =>
                    !char.IsAsciiLetterOrDigit(character) && character != '_'))
            {
                throw new ArgumentException(
                    "Enter a valid @username using 3–30 letters, numbers or underscores.");
            }

            existingUserId =
                await _invitationRepository.FindUserIdByUserNameAsync(
                    userName,
                    cancellationToken);

            if (!existingUserId.HasValue)
            {
                throw new InvalidOperationException(
                    $"No Shandis member was found with the username @{userName}. You can invite someone who has not joined yet using their email address.");
            }

            var registeredEmail = await _invitationRepository.GetUserEmailAsync(
                existingUserId.Value,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(registeredEmail))
                throw new InvalidOperationException("The selected Shandis member does not have a usable email address.");

            email = registeredEmail.Trim().ToLowerInvariant();
        }
        else
        {
            if (!new EmailAddressAttribute().IsValid(invitee))
                throw new ArgumentException("Enter a valid email address or a Shandis @username.");

            email = invitee.ToLowerInvariant();
            existingUserId =
                await _invitationRepository.FindUserIdByEmailAsync(
                    email,
                    cancellationToken);
        }

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

        if (existingUserId == command.InvitedByUserId)
            throw new InvalidOperationException(
                "You cannot invite yourself to your own Memory.");

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

        var persistedInvitationId = await _invitationRepository.UpsertInvitationAsync(
            invitation,
            existingUserId,
            cancellationToken);

        // Registered members receive an in-app notification. Email is retained
        // for invitations sent to people who have not created a Shandis account yet.
        if (!existingUserId.HasValue)
        {
            await _emailService.SendMemoryInvitationAsync(
                email,
                memoryTitle,
                rawToken,
                expiresAtUtc,
                cancellationToken);
        }

        if (existingUserId.HasValue)
        {
            await _notificationService.CreateAsync(
                existingUserId.Value,
                command.InvitedByUserId,
                NotificationType.MemoryInvitationReceived,
                "Memory invitation",
                $"Your invitation to \"{memoryTitle}\" has been updated. Accept it to join as a {(command.AssignedRole == InvitationRole.Contributor ? "Contributor" : "Viewer")}.",
                "/shared",
                command.MemoryId,
                cancellationToken: cancellationToken);
        }

        return new CreateInvitationResult
        {
            Id = persistedInvitationId,
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

        await _notificationService.CreateAsync(
            invitation.InvitedByUserId,
            userId,
            NotificationType.InvitationAccepted,
            "Invitation accepted",
            $"{userEmail.Trim()} accepted your invitation to \"{invitation.Memory?.Title ?? "your Memory"}\".",
            $"/memories/{invitation.MemoryId}",
            invitation.MemoryId,
            cancellationToken: cancellationToken);
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

        await _notificationService.CreateAsync(
            invitation.InvitedByUserId,
            userId,
            NotificationType.InvitationDeclined,
            "Invitation declined",
            $"{userEmail.Trim()} declined your invitation to \"{invitation.Memory?.Title ?? "your Memory"}\".",
            $"/memories/{invitation.MemoryId}",
            invitation.MemoryId,
            cancellationToken: cancellationToken);
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
