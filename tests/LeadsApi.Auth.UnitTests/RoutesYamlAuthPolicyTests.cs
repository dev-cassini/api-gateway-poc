using NUnit.Framework;

namespace LeadsApi.Auth.UnitTests;

[TestFixture]
public sealed class RoutesYamlAuthPolicyTests
{
    private ApiRoutesPolicy _routes = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _routes = RoutesYamlPolicyLoader.Load();
    }

    [Test]
    public void CreateLeadRoute_RequiresAuthenticatedUserAndLeadsImportScope()
    {
        var apiRoute = _routes.GetRoute("POST", "/leads");

        Assert.That(apiRoute.Plugins.Auth.RequiresAuthenticationUser, Is.True);
        Assert.That(apiRoute.Plugins.Auth.RequiredScopes, Does.Contain("leads:import"));
        Assert.That(apiRoute.Plugins.Auth.RequiredRoles, Is.Empty);
    }

    [Test]
    public void GetLeadRoute_RequiresAdviserOrCustomerRole()
    {
        var apiRoute = _routes.GetRoute("GET", "/leads/{leadId}");

        Assert.That(apiRoute.Plugins.Auth.RequiresAuthenticationUser, Is.True);
        Assert.That(apiRoute.Plugins.Auth.RequiredRoles, Is.EquivalentTo(new[] { "adviser", "customer" }));
    }

    [Test]
    public void AssignLeadRoute_RequiresAdviserAndManagerStaffTypeInOpa()
    {
        var apiRoute = _routes.GetRoute("POST", "/leads/{leadId}/assign");

        Assert.That(apiRoute.Plugins.Auth.RequiresAuthenticationUser, Is.True);
        Assert.That(apiRoute.Plugins.Auth.RequiredRoles, Is.EquivalentTo(new[] { "adviser" }));
        Assert.That(apiRoute.Plugins.Opa.RequiredStaffTypes, Is.EquivalentTo(new[] { "manager" }));
    }
}
