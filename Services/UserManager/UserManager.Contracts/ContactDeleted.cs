namespace UserManager.Contracts;

public sealed record ContactDeleted
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid ContactId { get; init; }
    public required Guid OwnerId { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public Guid? PerformedBy { get; init; }
}
