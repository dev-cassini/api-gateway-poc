using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadsApi.Models;
using NUnit.Framework;

namespace LeadsApi.ComponentTests;

[TestFixture]
public sealed class GetLeadTests
{
    [Test]
    public async Task GetLead_WithCustomerRole_Returns200()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var createdLead = await TestLeadHelper.CreateLeadAndGetResponseAsync(client);

        var token = TestJwtTokenFactory.CreateToken(
            userId: "customer-17",
            roles: ["customer"],
            scopes: ["profile:read"],
            email: "customer-17@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await client.GetAsync($"/leads/{createdLead.Id}");
        var payload = await response.Content.ReadFromJsonAsync<Lead>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Id, Is.EqualTo(createdLead.Id));
        Assert.That(payload.ContactName, Is.EqualTo("Lead Seed"));
        Assert.That(payload.Email, Is.EqualTo("seed@example.com"));
        Assert.That(payload.CreatedBy, Is.EqualTo("creator-01@example.com"));
        Assert.That(payload.AssignedAdviserId, Is.Null);
        Assert.That(payload.CreatedUtc, Is.EqualTo(createdLead.CreatedUtc));
    }
}
