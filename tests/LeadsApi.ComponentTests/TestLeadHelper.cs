using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LeadsApi.Contracts;
using NUnit.Framework;

namespace LeadsApi.ComponentTests;

internal static class TestLeadHelper
{
    public static async Task<Guid> CreateLeadAsync(HttpClient client)
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
        return document.RootElement.GetProperty("id").GetGuid();
    }
}
