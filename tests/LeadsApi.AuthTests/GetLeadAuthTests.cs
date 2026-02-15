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
        var token = TestJwtTokenFactory.CreateToken(
            userId: "ops-01",
            roles: ["operations"],
            scopes: ["leads:import"],
            email: "ops-01@example.com");
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await AuthTestContext.RequiredClient.GetAsync($"/leads/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetLead_WithAdviserRole_Returns200()
    {
        var createdLead = await TestLeadHelper.CreateLeadAndGetResponseAsync(AuthTestContext.RequiredClient);

        var token = TestJwtTokenFactory.CreateToken(
            userId: "adviser-01",
            roles: ["adviser"],
            scopes: ["profile:read"],
            email: "adviser-01@example.com");
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await AuthTestContext.RequiredClient.GetAsync($"/leads/{createdLead.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
