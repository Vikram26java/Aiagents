using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessAiAgent.Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly AppDbContext _db;

    public ContactService(AppDbContext db) => _db = db;

    public async Task<Contact> GetOrCreateContactAsync(string? name = null, string? email = null, string? phone = null)
    {
        Contact? contact = null;

        if (!string.IsNullOrEmpty(email))
            contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Email == email);

        if (contact == null && !string.IsNullOrEmpty(phone))
            contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Phone == phone);

        if (contact != null)
            return contact;

        contact = new Contact
        {
            Name = name ?? email ?? phone ?? "Unknown",
            Email = email,
            Phone = phone
        };
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();
        return contact;
    }

    public async Task<Contact?> GetContactAsync(int id)
    {
        return await _db.Contacts
            .Include(c => c.Company)
            .Include(c => c.Conversations)
                .ThenInclude(conv => conv.Messages)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Contact>> GetContactsAsync(string? search = null)
    {
        var query = _db.Contacts
            .Include(c => c.Company)
            .Include(c => c.Conversations)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(s) ||
                (c.Email != null && c.Email.ToLower().Contains(s)) ||
                (c.Phone != null && c.Phone.Contains(s)));
        }

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<Contact> UpdateContactAsync(int id, string name, string? email, string? phone, int? companyId)
    {
        var contact = await _db.Contacts.FindAsync(id)
            ?? throw new KeyNotFoundException($"Contact {id} not found");

        contact.Name = name;
        contact.Email = email;
        contact.Phone = phone;
        contact.CompanyId = companyId;
        contact.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return contact;
    }

    public async Task DeleteContactAsync(int id)
    {
        var contact = await _db.Contacts.FindAsync(id)
            ?? throw new KeyNotFoundException($"Contact {id} not found");

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync();
    }
}
