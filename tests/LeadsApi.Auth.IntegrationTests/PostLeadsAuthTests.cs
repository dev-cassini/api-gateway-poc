using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadsApi.Contracts;
using NUnit.Framework;

namespace LeadsApi.Auth.IntegrationTests;

[TestFixture]
public sealed class PostLeadsAuthTests
{
    [SetUp]
    public void SetUp()
    {
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = null;
    }

    [Test]
    public async Task PostLeads_WithoutToken_Returns401()
    {
        var response = await AuthTestContext.RequiredClient.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostLeads_WithMissingScope_Returns403()
    {
        var token = TestJwtTokenFactory.CreateToken(
            userId: "adviser-01",
            roles: ["adviser"],
            scopes: ["profile:read"],
            email: "adviser-01@example.com");
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await AuthTestContext.RequiredClient.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PostLeads_WithRequiredScope_Returns201()
    {
        var token = TestJwtTokenFactory.CreateToken(
            userId: "adviser-01",
            roles: ["adviser"],
            scopes: ["leads:import"],
            email: "adviser-01@example.com");
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await AuthTestContext.RequiredClient.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }
}
