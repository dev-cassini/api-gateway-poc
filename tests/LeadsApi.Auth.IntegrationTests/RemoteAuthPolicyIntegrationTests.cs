using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadsApi.Contracts;
using LeadsApi.Models;
using NUnit.Framework;

namespace LeadsApi.Auth.IntegrationTests;

[TestFixture]
public sealed class RemoteAuthPolicyIntegrationTests
{
    [SetUp]
    public void SetUp()
    {
        RemoteIntegrationContext.KongClient.DefaultRequestHeaders.Authorization = null;
    }

    [Test]
    public async Task PostLeads_WithoutToken_Returns401()
    {
        var response = await RemoteIntegrationContext.KongClient.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostLeads_WithMissingScope_Returns403()
    {
        await AuthorizeAsync(
            RemoteIntegrationContext.Settings.AdviserUser,
            RemoteIntegrationContext.Settings.ReadScope);

        var response = await RemoteIntegrationContext.KongClient.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PostLeads_WithImportScope_Returns201()
    {
        await AuthorizeAsync(
            RemoteIntegrationContext.Settings.AdviserUser,
            RemoteIntegrationContext.Settings.LeadImportScope);

        var response = await RemoteIntegrationContext.KongClient.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Jane Doe",
            Email = "jane@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task GetLead_WithWrongRole_Returns403()
    {
        await AuthorizeAsync(
            RemoteIntegrationContext.Settings.NonPrivilegedUser,
            RemoteIntegrationContext.Settings.ReadScope);

        var response = await RemoteIntegrationContext.KongClient.GetAsync($"/leads/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetLead_WithCustomerRole_Returns200()
    {
        var leadId = await CreateLeadAsync();
        await AuthorizeAsync(
            RemoteIntegrationContext.Settings.CustomerUser,
            RemoteIntegrationContext.Settings.ReadScope);

        var response = await RemoteIntegrationContext.KongClient.GetAsync($"/leads/{leadId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task AssignLead_WithNonManager_Returns403()
    {
        var leadId = await CreateLeadAsync();
        await AuthorizeAsync(
            RemoteIntegrationContext.Settings.AdviserUser,
            RemoteIntegrationContext.Settings.ReadScope);

        var response = await RemoteIntegrationContext.KongClient.PostAsJsonAsync($"/leads/{leadId}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AssignLead_WithManager_Returns200()
    {
        var leadId = await CreateLeadAsync();
        await AuthorizeAsync(
            RemoteIntegrationContext.Settings.ManagerUser,
            RemoteIntegrationContext.Settings.ReadScope);

        var response = await RemoteIntegrationContext.KongClient.PostAsJsonAsync($"/leads/{leadId}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private static async Task<Guid> CreateLeadAsync()
    {
        await AuthorizeAsync(
            RemoteIntegrationContext.Settings.AdviserUser,
            RemoteIntegrationContext.Settings.LeadImportScope);

        var response = await RemoteIntegrationContext.KongClient.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Lead Seed",
            Email = "seed@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var lead = await response.Content.ReadFromJsonAsync<Lead>();
        Assert.That(lead, Is.Not.Null);
        return lead!.Id;
    }

    private static async Task AuthorizeAsync(string userName, string scope)
    {
        var token = await RemoteIntegrationContext.TokenProvider.GetUserTokenAsync(
            userName,
            scope,
            CancellationToken.None);

        RemoteIntegrationContext.KongClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}
