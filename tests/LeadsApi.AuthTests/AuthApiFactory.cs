using LeadsApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LeadsApi.AuthTests;

internal sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;

    public Uri ServerUri { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("AuthTesting:ShortCircuitEnabled", "true"),
                new KeyValuePair<string, string?>("AuthTesting:CreatedPaths:0", "/leads")
            ]);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IStaffTypeClient>();
            services.AddSingleton<IStaffTypeClient>(new StubStaffTypeClient());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Keep TestServer host for WebApplicationFactory internals.
        var testHost = builder.Build();

        // Start a real Kestrel host so Kong can proxy to it.
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls("http://0.0.0.0:0");
        });

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var server = _kestrelHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        var address = addresses?.FirstOrDefault(static a => !a.EndsWith(":0", StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("Failed to determine Kestrel address for auth tests.");
        }

        var port = new Uri(address).Port;
        ServerUri = new Uri($"http://127.0.0.1:{port}");

        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _kestrelHost?.Dispose();
            _kestrelHost = null;
        }
    }

    private sealed class StubStaffTypeClient : IStaffTypeClient
    {
        public Task<string?> GetStaffTypeAsync(string userId, CancellationToken cancellationToken)
        {
            var staffType = userId.Contains("manager", StringComparison.OrdinalIgnoreCase)
                ? "manager"
                : "staff";

            return Task.FromResult<string?>(staffType);
        }
    }
}
