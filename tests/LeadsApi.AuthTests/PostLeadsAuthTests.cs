using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadsApi.Contracts;
using NUnit.Framework;

namespace LeadsApi.AuthTests;

[TestFixture]
public sealed class PostLeadsAuthTests
{
    [Test]
    public async Task PostLeads_WithoutToken_Returns401()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostLeads_WithMissingScope_Returns403()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "adviser-01|adviser|profile:read|adviser-01@example.com");

        var response = await client.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
