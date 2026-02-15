namespace LeadsApi.Services;

public interface IStaffTypeClient
{
    Task<string?> GetStaffTypeAsync(string userId, CancellationToken cancellationToken);
}
