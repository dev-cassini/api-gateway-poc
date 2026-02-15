using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadsApi.Contracts;
using LeadsApi.Models;
using NUnit.Framework;

namespace LeadsApi.ComponentTests;

[TestFixture]
public sealed class AssignLeadTests
{
    [Test]
    public async Task AssignLead_WithAdviserManager_Returns200()
    {
        await using var factory = new ApiFactory("manager");
        using var client = factory.CreateClient();
        var createdLead = await TestLeadHelper.CreateLeadAndGetResponseAsync(client);

        var token = TestJwtTokenFactory.CreateToken(
            userId: "adviser-01",
            roles: ["adviser"],
            scopes: ["profile:read"],
            email: "adviser-01@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await client.PostAsJsonAsync($"/leads/{createdLead.Id}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });
        var payload = await response.Content.ReadFromJsonAsync<Lead>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Id, Is.EqualTo(createdLead.Id));
        Assert.That(payload.ContactName, Is.EqualTo(createdLead.ContactName));
        Assert.That(payload.Email, Is.EqualTo(createdLead.Email));
        Assert.That(payload.CreatedBy, Is.EqualTo(createdLead.CreatedBy));
        Assert.That(payload.AssignedAdviserId, Is.EqualTo("adviser-99"));
        Assert.That(payload.CreatedUtc, Is.EqualTo(createdLead.CreatedUtc));
    }
}
