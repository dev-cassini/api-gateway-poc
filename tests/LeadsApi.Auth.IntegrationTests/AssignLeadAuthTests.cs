using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadsApi.Contracts;
using NUnit.Framework;

namespace LeadsApi.Auth.IntegrationTests;

[TestFixture]
public sealed class AssignLeadAuthTests
{
    [SetUp]
    public void SetUp()
    {
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = null;
    }

    [Test]
    public async Task AssignLead_WithoutToken_Returns401()
    {
        var response = await AuthTestContext.RequiredClient.PostAsJsonAsync($"/leads/{Guid.NewGuid()}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AssignLead_WhenStaffTypeIsNotManager_Returns403()
    {
        var token = TestJwtTokenFactory.CreateToken(
            userId: "adviser-01",
            roles: ["adviser"],
            scopes: ["profile:read"],
            email: "adviser-01@example.com");
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await AuthTestContext.RequiredClient.PostAsJsonAsync($"/leads/{Guid.NewGuid()}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AssignLead_WithAdviserManager_Returns200()
    {
        var createdLead = await TestLeadHelper.CreateLeadAndGetResponseAsync(AuthTestContext.RequiredClient);

        var token = TestJwtTokenFactory.CreateToken(
            userId: "manager-01",
            roles: ["adviser"],
            scopes: ["profile:read"],
            email: "manager-01@example.com");
        AuthTestContext.RequiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await AuthTestContext.RequiredClient.PostAsJsonAsync($"/leads/{createdLead.Id}/assign", new AssignLeadRequest
        {
            AdviserId = "adviser-99"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
