using System.Net;
using System.Net.Http.Headers;
using NUnit.Framework;

namespace LeadsApi.AuthTests;

[TestFixture]
public sealed class GetLeadAuthTests
{
    [SetUp]
    public void SetUp()
    {
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = null;
    }

    [Test]
    public async Task GetLead_WithoutToken_Returns401()
    {
        var response = await AuthTestContext.RequiredClient.GetAsync($"/leads/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetLead_WithWrongRole_Returns403()
    {
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "ops-01|operations|leads:import|ops-01@example.com");

        var response = await AuthTestContext.RequiredClient.GetAsync($"/leads/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetLead_WithAdviserRole_Returns200()
    {
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "adviser-01|adviser|profile:read|adviser-01@example.com");

        var response = await AuthTestContext.RequiredClient.GetAsync($"/leads/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
