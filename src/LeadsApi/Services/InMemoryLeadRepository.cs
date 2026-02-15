using System.Collections.Concurrent;
using LeadsApi.Contracts;
using LeadsApi.Models;

namespace LeadsApi.Services;

public sealed class InMemoryLeadRepository : ILeadRepository
{
    private readonly ConcurrentDictionary<Guid, Lead> _leads = new();

    public Lead Create(CreateLeadRequest request)
    {
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            ContactName = request.ContactName.Trim(),
            Email = request.Email.Trim(),
            CreatedUtc = DateTimeOffset.UtcNow
        };

        _leads[lead.Id] = lead;
        return lead;
    }

    public Lead? GetById(Guid leadId)
    {
        return _leads.TryGetValue(leadId, out var lead) ? lead : null;
    }

    public Lead? Assign(Guid leadId, string adviserId)
    {
        if (!_leads.TryGetValue(leadId, out var existing))
        {
            return null;
        }

        var updated = existing with
        {
            AssignedAdviserId = adviserId.Trim()
        };

        _leads[leadId] = updated;
        return updated;
    }
}
