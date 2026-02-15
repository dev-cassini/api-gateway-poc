using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadsApi.Contracts;
using LeadsApi.Models;
using NUnit.Framework;

namespace LeadsApi.ComponentTests;

[TestFixture]
public sealed class PostLeadsTests
{
    [Test]
    public async Task PostLeads_WithLeadsImportScope_Returns200()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var token = TestJwtTokenFactory.CreateToken(
            userId: "adviser-01",
            roles: ["adviser"],
            scopes: ["leads:import"],
            email: "adviser-01@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await client.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });
        var payload = await response.Content.ReadFromJsonAsync<Lead>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(payload.ContactName, Is.EqualTo("Jane Doe"));
        Assert.That(payload.Email, Is.EqualTo("jane@example.com"));
        Assert.That(payload.CreatedBy, Is.EqualTo("adviser-01@example.com"));
        Assert.That(payload.AssignedAdviserId, Is.Null);
        Assert.That(payload.CreatedUtc, Is.Not.EqualTo(default(DateTimeOffset)));
    }
}
