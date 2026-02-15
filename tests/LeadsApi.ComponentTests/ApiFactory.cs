using LeadsApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LeadsApi.ComponentTests;

internal sealed class ApiFactory(string? staffType = "manager") : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Jwt:SigningKey", TestJwtTokenFactory.SigningKey)
            ]);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IStaffTypeClient>();
            services.AddSingleton<IStaffTypeClient>(new StubStaffTypeClient(staffType));
        });
    }

    private sealed class StubStaffTypeClient(string? staffType) : IStaffTypeClient
    {
        public Task<string?> GetStaffTypeAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(staffType);
        }
    }
}
