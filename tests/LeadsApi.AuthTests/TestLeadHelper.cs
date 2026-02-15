using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LeadsApi.Contracts;
using LeadsApi.Models;
using NUnit.Framework;

namespace LeadsApi.AuthTests;

internal static class TestLeadHelper
{
    public static async Task<Lead> CreateLeadAndGetResponseAsync(HttpClient client)
    {
        var token = TestJwtTokenFactory.CreateToken(
            userId: "creator-01",
            roles: ["adviser"],
            scopes: ["leads:import"],
            email: "creator-01@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);

        var response = await client.PostAsJsonAsync("/leads", new CreateLeadRequest
        {
            ContactName = "Lead Seed",
            Email = "seed@example.com"
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var payload = await response.Content.ReadFromJsonAsync<Lead>();
        Assert.That(payload, Is.Not.Null);

        return payload!;
    }
}
