using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Services.Interfaces
{
    public interface IOrganizationResolver
    {
        Task<Organization?> GetCurrentOrganizationAsync();
    }
}
