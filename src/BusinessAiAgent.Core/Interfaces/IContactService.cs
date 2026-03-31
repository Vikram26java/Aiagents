using BusinessAiAgent.Core.Entities;

namespace BusinessAiAgent.Core.Interfaces;

public interface IContactService
{
    Task<Contact> GetOrCreateContactAsync(string? name = null, string? email = null, string? phone = null);
    Task<Contact?> GetContactAsync(int id);
    Task<List<Contact>> GetContactsAsync(string? search = null);
    Task<Contact> UpdateContactAsync(int id, string name, string? email, string? phone, int? companyId);
    Task DeleteContactAsync(int id);
}
