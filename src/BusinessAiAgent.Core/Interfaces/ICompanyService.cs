using BusinessAiAgent.Core.Entities;

namespace BusinessAiAgent.Core.Interfaces;

public interface ICompanyService
{
    Task<List<Company>> GetAllAsync();
    Task<Company?> GetAsync(int id);
    Task<Company> CreateAsync(string name, string? address);
    Task<Company> UpdateAsync(int id, string name, string? address);
    Task DeleteAsync(int id);
    Task<int> GetMemberCountAsync(int companyId);
}
