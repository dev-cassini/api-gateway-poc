using LeadsApi.Contracts;
using LeadsApi.Models;
using LeadsApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LeadsApi.AuthTests;

internal sealed class AuthApiFactory(string? staffType = "manager") : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILeadRepository>();
            services.AddSingleton<ILeadRepository>(new StubLeadRepository());

            services.RemoveAll<IStaffTypeClient>();
            services.AddSingleton<IStaffTypeClient>(new StubStaffTypeClient(staffType));
        });
    }

    private sealed class StubStaffTypeClient(string? staffType) : IStaffTypeClient
    {
        public Task<string?> GetStaffTypeAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(staffType);
        }
    }

    private sealed class StubLeadRepository : ILeadRepository
    {
        public Lead Create(CreateLeadRequest request, string? createdBy)
        {
            return new Lead
            {
                Id = Guid.NewGuid(),
                ContactName = request.ContactName,
                Email = request.Email,
                CreatedBy = createdBy,
                CreatedUtc = DateTimeOffset.UtcNow
            };
        }

        public Lead? GetById(Guid leadId)
        {
            return new Lead
            {
                Id = leadId,
                ContactName = "Auth Test Lead",
                Email = "auth-test@example.com",
                CreatedBy = "auth-test@example.com",
                CreatedUtc = DateTimeOffset.UtcNow
            };
        }

        public Lead? Assign(Guid leadId, string adviserId)
        {
            return new Lead
            {
                Id = leadId,
                ContactName = "Auth Test Lead",
                Email = "auth-test@example.com",
                CreatedBy = "auth-test@example.com",
                CreatedUtc = DateTimeOffset.UtcNow,
                AssignedAdviserId = adviserId
            };
        }
    }
}
