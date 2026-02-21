using System.Text;
using LeadsApi.Endpoints;
using LeadsApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? "local-dev-jwt-signing-key-for-poc-2026";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddSingleton<ILeadRepository, InMemoryLeadRepository>();
builder.Services.AddHttpClient<IStaffTypeClient, StaffTypeClient>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["StaffDirectory:BaseUrl"];

    if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
    {
        client.BaseAddress = uri;
    }

    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.UseAuthentication();
app.MapLeadEndpoints();

app.Run();

public partial class Program
{
}
