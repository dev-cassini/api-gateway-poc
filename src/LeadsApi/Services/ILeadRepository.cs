using LeadsApi.Contracts;
using LeadsApi.Models;

namespace LeadsApi.Services;

public interface ILeadRepository
{
    Lead Create(CreateLeadRequest request, string? createdBy);
    Lead? GetById(Guid leadId);
    Lead? Assign(Guid leadId, string adviserId);
}
