namespace LeadsApi.Endpoints;

public static class Extensions
{
    public static void MapLeadEndpoints(this WebApplication app)
    {
        var leads = app.MapGroup("/leads");
        leads.MapCreateLeadEndpoint();
        leads.MapGetLeadEndpoint();
        leads.MapAssignLeadEndpoint();
    }
}
