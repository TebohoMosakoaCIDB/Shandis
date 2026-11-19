using Shandis.Domain.Entities;

namespace Shandis.Application.Abstractions.Persistence;

public interface IInvitationRepository
{
    Task<Guid?> GetMemoryOwnerIdAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default);

    Task<Guid?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<Guid?> FindUserIdByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingInvitationAsync(
        Guid memoryId,
        string email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MemoryInvitation invitation,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryInvitation>> GetForMemoryAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryInvitation>> GetReceivedAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default);

    Task<string?> GetUserEmailAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<string?> GetMemoryTitleAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default);

    Task<MemoryInvitation?> GetByIdAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default);

    Task<MemoryParticipant?> GetParticipantAsync(
        Guid memoryId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AcceptAsync(
        MemoryInvitation invitation,
        MemoryParticipant participant,
        bool isExistingParticipant,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        MemoryInvitation invitation,
        CancellationToken cancellationToken = default);

    Task<int> ExpirePendingInvitationsAsync(
        Guid? memoryId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
