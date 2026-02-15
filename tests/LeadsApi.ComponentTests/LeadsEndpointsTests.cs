using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LeadsApi.Contracts;
using NUnit.Framework;

namespace LeadsApi.ComponentTests;

[TestFixture]
public sealed class LeadsEndpointsTests
{
    [Test]
    public async Task PostLeads_WithoutToken_Returns401()
    {
        await using var factory = new ApiFactory();
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
        await using var factory = new ApiFactory();
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

    [Test]
    public async Task PostLeads_WithLeadsImportScope_Returns200()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "adviser-01|adviser|leads:import|adviser-01@example.com");

        var response = await client.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

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
        var leadId = await CreateLeadAsync(client);

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
        var leadId = await CreateLeadAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "customer-17|customer|profile:read|customer-17@example.com");

        var response = await client.GetAsync($"/leads/{leadId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task AssignLead_WithoutToken_Returns401()
    {
        await using var factory = new ApiFactory();
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
        await using var factory = new ApiFactory("staff");
        using var client = factory.CreateClient();
        var leadId = await CreateLeadAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "adviser-01|adviser|profile:read|adviser-01@example.com");

        var response = await client.PostAsJsonAsync($"/leads/{leadId}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AssignLead_WithAdviserManager_Returns200()
    {
        await using var factory = new ApiFactory("manager");
        using var client = factory.CreateClient();
        var leadId = await CreateLeadAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "adviser-01|adviser|profile:read|adviser-01@example.com");

        var response = await client.PostAsJsonAsync($"/leads/{leadId}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private static async Task<Guid> CreateLeadAsync(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "creator-01|adviser|leads:import|creator-01@example.com");

        var response = await client.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Lead Seed",
            Email = "seed@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = document.RootElement.GetProperty("id").GetGuid();

        return id;
    }
}
