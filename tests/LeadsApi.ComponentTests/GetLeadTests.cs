using System.Net;
using System.Net.Http.Headers;
using NUnit.Framework;

namespace LeadsApi.ComponentTests;

[TestFixture]
public sealed class GetLeadTests
{
    [Test]
    public async Task GetLead_WithoutToken_Returns401()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/leads/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetLead_WithWrongRole_Returns403()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var leadId = await TestLeadHelper.CreateLeadAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "ops-01|operations|leads:import|ops-01@example.com");

        var response = await client.GetAsync($"/leads/{leadId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetLead_WithCustomerRole_Returns200()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var leadId = await TestLeadHelper.CreateLeadAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "customer-17|customer|profile:read|customer-17@example.com");

        var response = await client.GetAsync($"/leads/{leadId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
