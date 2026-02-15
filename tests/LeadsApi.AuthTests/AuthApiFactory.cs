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
            services.AddSingleton<ILeadRepository>(new ThrowingLeadRepository());

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

    private sealed class ThrowingLeadRepository : ILeadRepository
    {
        public Lead Create(CreateLeadRequest request, string? createdBy)
        {
            throw new InvalidOperationException("Lead repository should not be called in auth-only tests.");
        }

        public Lead? GetById(Guid leadId)
        {
            throw new InvalidOperationException("Lead repository should not be called in auth-only tests.");
        }

        public Lead? Assign(Guid leadId, string adviserId)
        {
            throw new InvalidOperationException("Lead repository should not be called in auth-only tests.");
        }
    }
}
