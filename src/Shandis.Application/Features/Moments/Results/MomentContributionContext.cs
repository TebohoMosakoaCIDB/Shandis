using Shandis.Domain.Entities;

namespace Shandis.Application.Features.Moments.Results;

public class MomentContributionContext
{
    public Memory Memory { get; set; } = null!;

    public MemoryParticipant? Participant { get; set; }
}