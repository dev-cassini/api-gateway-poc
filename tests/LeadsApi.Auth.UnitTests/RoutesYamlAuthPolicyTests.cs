using NUnit.Framework;

namespace LeadsApi.Auth.UnitTests;

[TestFixture]
public sealed class RoutesYamlAuthPolicyTests
{
    [Test]
    public void CreateLeadRoute_RequiresAuthenticatedUserAndLeadsImportScope()
    {
        var routes = RoutesYamlPolicyLoader.Load();
        var apiRoute = routes.GetRoute("POST", "/leads");

        Assert.That(apiRoute.Plugins.Auth.RequiresAuthenticationUser, Is.True);
        Assert.That(apiRoute.Plugins.Auth.RequiredScopes, Does.Contain("leads:import"));
        Assert.That(apiRoute.Plugins.Auth.RequiredRoles, Is.Empty);
    }

    [Test]
    public void GetLeadRoute_RequiresAdviserOrCustomerRole()
    {
        var routes = RoutesYamlPolicyLoader.Load();
        var apiRoute = routes.GetRoute("GET", "/leads/{leadId}");

        Assert.That(apiRoute.Plugins.Auth.RequiresAuthenticationUser, Is.True);
        Assert.That(apiRoute.Plugins.Auth.RequiredRoles, Is.EquivalentTo(new[] { "adviser", "customer" }));
    }

    [Test]
    public void AssignLeadRoute_RequiresAdviserAndManagerStaffTypeInOpa()
    {
        var routes = RoutesYamlPolicyLoader.Load();
        var apiRoute = routes.GetRoute("POST", "/leads/{leadId}/assign");

        Assert.That(apiRoute.Plugins.Auth.RequiresAuthenticationUser, Is.True);
        Assert.That(apiRoute.Plugins.Auth.RequiredRoles, Is.EquivalentTo(new[] { "adviser" }));
        Assert.That(apiRoute.Plugins.Opa.RequiredStaffTypes, Is.EquivalentTo(new[] { "manager" }));
    }
}
