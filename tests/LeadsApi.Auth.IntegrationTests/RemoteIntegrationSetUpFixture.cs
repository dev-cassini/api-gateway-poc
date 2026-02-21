using NUnit.Framework;

namespace LeadsApi.Auth.IntegrationTests;

[SetUpFixture]
public sealed class RemoteIntegrationSetUpFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (!RemoteIntegrationSettings.TryLoadFromEnvironment(out var settings, out var error))
        {
            Assert.Ignore($"Remote auth integration tests skipped: {error}");
            return;
        }

        if (!string.Equals(settings!.EnvironmentName, "qa", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore($"Remote auth integration tests skipped: AUTH_TEST_ENV must be 'qa' but was '{settings.EnvironmentName}'.");
            return;
        }

        var kongClient = new HttpClient
        {
            BaseAddress = settings.BaseUrl
        };

        var idpClient = new HttpClient();

        RemoteIntegrationContext.Initialize(settings, kongClient, new ImpersonationTokenProvider(idpClient, settings));
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        RemoteIntegrationContext.Dispose();
    }
}

internal static class RemoteIntegrationContext
{
    public static RemoteIntegrationSettings Settings { get; private set; } = null!;
    public static HttpClient KongClient { get; private set; } = null!;
    public static IImpersonationTokenProvider TokenProvider { get; private set; } = null!;

    public static void Initialize(
        RemoteIntegrationSettings settings,
        HttpClient kongClient,
        IImpersonationTokenProvider tokenProvider)
    {
        Settings = settings;
        KongClient = kongClient;
        TokenProvider = tokenProvider;
    }

    public static void Dispose()
    {
        KongClient?.Dispose();
        KongClient = null!;
        TokenProvider = null!;
        Settings = null!;
    }
}
