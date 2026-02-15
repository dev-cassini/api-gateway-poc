namespace LeadsApi.Contracts;

public sealed class CreateLeadRequest
{
    public string ContactName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
