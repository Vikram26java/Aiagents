using BusinessAiAgent.Core.Entities;
using BusinessAiAgent.Core.Interfaces;
using BusinessAiAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessAiAgent.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly AppDbContext _db;

    public CompanyService(AppDbContext db) => _db = db;

    public async Task<List<Company>> GetAllAsync()
    {
        return await _db.Companies
            .Include(c => c.Contacts)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Company?> GetAsync(int id)
    {
        return await _db.Companies
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company> CreateAsync(string name, string? address)
    {
        var company = new Company
        {
            Name = name,
            Address = address
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();
        return company;
    }

    public async Task<Company> UpdateAsync(int id, string name, string? address)
    {
        var company = await _db.Companies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Company {id} not found");

        company.Name = name;
        company.Address = address;
        company.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return company;
    }

    public async Task DeleteAsync(int id)
    {
        var company = await _db.Companies.FindAsync(id)
            ?? throw new KeyNotFoundException($"Company {id} not found");

        _db.Companies.Remove(company);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetMemberCountAsync(int companyId)
    {
        return await _db.Contacts.CountAsync(c => c.CompanyId == companyId);
    }
}
