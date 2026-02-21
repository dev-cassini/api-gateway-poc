using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using System.Net;

namespace LeadsApi.Auth.IntegrationTests;

[SetUpFixture]
public sealed class WebApplicationSetUpFixture
{
    private const string KongTemplateFileName = "kong.declarative.template.yaml";
    private const string KongImage = "kong:latest";

    private AuthApiFactory _apiFactory = null!;
    private IContainer _kongContainer = null!;
    private string _kongConfigFilePath = string.Empty;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _apiFactory = new AuthApiFactory();

        using var warmupClient = _apiFactory.CreateClient();
        var apiBaseAddress = _apiFactory.ServerUri;

        await WaitForApiReadyAsync(apiBaseAddress);

        _kongConfigFilePath = CreateKongConfigFile(apiBaseAddress);

        _kongContainer = new ContainerBuilder()
            .WithImage(KongImage)
            .WithPortBinding(8000, true)
            .WithEnvironment("KONG_DATABASE", "off")
            .WithEnvironment("KONG_DECLARATIVE_CONFIG", "/kong/declarative/kong.yaml")
            .WithEnvironment("KONG_PROXY_ACCESS_LOG", "/dev/stdout")
            .WithEnvironment("KONG_ADMIN_ACCESS_LOG", "/dev/stdout")
            .WithEnvironment("KONG_PROXY_ERROR_LOG", "/dev/stderr")
            .WithEnvironment("KONG_ADMIN_ERROR_LOG", "/dev/stderr")
            .WithEnvironment("KONG_ADMIN_LISTEN", "off")
            .WithBindMount(_kongConfigFilePath, "/kong/declarative/kong.yaml", AccessMode.ReadOnly)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8000))
            .Build();

        using var kongStartCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await _kongContainer.StartAsync(kongStartCancellation.Token);

        var kongPort = _kongContainer.GetMappedPublicPort(8000);
        await WaitForKongReadyAsync(kongPort);

        AuthTestContext.Client = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{kongPort}")
        };
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        AuthTestContext.Client?.Dispose();
        AuthTestContext.Client = null;

        if (_kongContainer is not null)
        {
            await _kongContainer.DisposeAsync();
        }

        _apiFactory.Dispose();

        if (!string.IsNullOrWhiteSpace(_kongConfigFilePath) && File.Exists(_kongConfigFilePath))
        {
            File.Delete(_kongConfigFilePath);
        }
    }

    private static string CreateKongConfigFile(Uri apiBaseAddress)
    {
        var apiPort = apiBaseAddress.Port;
        var upstreamHost = Environment.GetEnvironmentVariable("AUTH_TESTS_UPSTREAM_HOST")
                           ?? "host.docker.internal";
        var templatePath = Path.Combine(AppContext.BaseDirectory, KongTemplateFileName);
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Could not find Kong template file at '{templatePath}'.");
        }

        var kongConfig = File.ReadAllText(templatePath)
            .Replace("{{UPSTREAM_HOST}}", upstreamHost, StringComparison.Ordinal)
            .Replace("{{UPSTREAM_PORT}}", apiPort.ToString(), StringComparison.Ordinal);

        var filePath = Path.Combine("/tmp", $"kong-auth-tests-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(filePath, kongConfig);
        return filePath;
    }

    private static async Task WaitForKongReadyAsync(ushort kongPort)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{kongPort}"),
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await httpClient.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);
                if (response.StatusCode != HttpStatusCode.ServiceUnavailable)
                {
                    return;
                }
            }
            catch
            {
                // Kong is still booting.
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException("Timed out waiting for Kong to become ready.");
    }

    private static async Task WaitForApiReadyAsync(Uri apiBaseAddress)
    {
        using var probeClient = new HttpClient
        {
            BaseAddress = apiBaseAddress,
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await probeClient.GetAsync($"/leads/{Guid.NewGuid()}");
                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch
            {
                // API is still starting.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Timed out waiting for API upstream at {apiBaseAddress}.");
    }
}

internal static class AuthTestContext
{
    public static HttpClient? Client { get; set; }

    public static HttpClient RequiredClient =>
        Client ?? throw new InvalidOperationException("Auth test HttpClient has not been initialized.");
}
