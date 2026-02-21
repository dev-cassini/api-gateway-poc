using NUnit.Framework;

namespace LeadsApi.Auth.UnitTests;

[TestFixture]
public sealed class RoutesYamlAuthCoverageTests
{
    private static readonly Lazy<ApiRoutesPolicy> Routes = new(static () => RoutesYamlPolicyLoader.Load());

    public static IEnumerable<TestCaseData> Endpoints()
    {
        foreach (var route in Routes.Value.Routes)
        {
            yield return new TestCaseData(route)
                .SetName($"Route_{route.Method}_{route.Path}_HasAuthPolicy");
        }
    }

    [TestCaseSource(nameof(Endpoints))]
    public void EachEndpoint_HasAuthPolicy(object route)
    {
        var apiRoute = (ApiRoutePolicy)route;
        Assert.That(apiRoute.Plugins.Auth.HasPolicy, Is.True,
            $"Route '{apiRoute.Method} {apiRoute.Path}' must define plugins.auth in routes.yaml.");
    }
}
