namespace Shandis.Api.Contracts.Profiles;

public sealed class MemberSearchResponse
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}
