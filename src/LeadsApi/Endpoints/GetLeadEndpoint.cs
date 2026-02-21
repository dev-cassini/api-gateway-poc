using LeadsApi.Services;

namespace LeadsApi.Endpoints;

public static class GetLeadEndpoint
{
    public static void MapGetLeadEndpoint(this RouteGroupBuilder leads)
    {
        leads.MapGet("/{leadId:guid}", Handle)
            .AllowAnonymous();
    }

    public static IResult Handle(Guid leadId, ILeadRepository repository)
    {
        var lead = repository.GetById(leadId);
        return lead is null ? Results.NotFound() : Results.Ok(lead);
    }
}
