using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadsApi.Contracts;
using NUnit.Framework;

namespace LeadsApi.AuthTests;

[TestFixture]
public sealed class AssignLeadAuthTests
{
    [Test]
    public async Task AssignLead_WithoutToken_Returns401()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/leads/{Guid.NewGuid()}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AssignLead_WhenStaffTypeIsNotManager_Returns403()
    {
        await using var factory = new AuthApiFactory("staff");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "adviser-01|adviser|profile:read|adviser-01@example.com");

        var response = await client.PostAsJsonAsync($"/leads/{Guid.NewGuid()}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AssignLead_WithAdviserManager_Returns200()
    {
        await using var factory = new AuthApiFactory("manager");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "adviser-01|adviser|profile:read|adviser-01@example.com");

        var response = await client.PostAsJsonAsync($"/leads/{Guid.NewGuid()}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
