namespace LeadsApi.Models;

public sealed record Lead
{
    public Guid Id { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; init; }
    public string? AssignedAdviserId { get; init; }
}
