using ad_tels.Data;
using ad_tels.Models;
using Microsoft.EntityFrameworkCore;

namespace ad_tels.Services;

public class ContactService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ContactService> _logger;

    public ContactService(ApplicationDbContext context, ILogger<ContactService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Contact>> GetAllContactsAsync(int page = 1, int pageSize = 50)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .OrderBy(c => c.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalContactsCountAsync()
    {
        return await _context.Contacts.CountAsync();
    }

    public async Task<Contact?> GetContactByIdAsync(int id)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Contact>> SearchContactsAsync(string searchTerm, int page = 1, int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllContactsAsync(page, pageSize);
        }

        searchTerm = searchTerm.ToLower();

        return await _context.Contacts
            .Include(c => c.LdapSource)
            .Where(c => 
                c.DisplayName.ToLower().Contains(searchTerm) ||
                c.FirstName.ToLower().Contains(searchTerm) ||
                c.LastName.ToLower().Contains(searchTerm) ||
                c.Email.ToLower().Contains(searchTerm) ||
                c.PhoneNumber.Contains(searchTerm) ||
                c.Mobile.Contains(searchTerm) ||
                c.Department.ToLower().Contains(searchTerm) ||
                c.Title.ToLower().Contains(searchTerm) ||
                c.Company.ToLower().Contains(searchTerm)
            )
            .OrderBy(c => c.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetSearchResultsCountAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetTotalContactsCountAsync();
        }

        searchTerm = searchTerm.ToLower();

        return await _context.Contacts
            .Where(c => 
                c.DisplayName.ToLower().Contains(searchTerm) ||
                c.FirstName.ToLower().Contains(searchTerm) ||
                c.LastName.ToLower().Contains(searchTerm) ||
                c.Email.ToLower().Contains(searchTerm) ||
                c.PhoneNumber.Contains(searchTerm) ||
                c.Mobile.Contains(searchTerm) ||
                c.Department.ToLower().Contains(searchTerm) ||
                c.Title.ToLower().Contains(searchTerm) ||
                c.Company.ToLower().Contains(searchTerm)
            )
            .CountAsync();
    }

    public async Task<IEnumerable<Contact>> GetContactsByDepartmentAsync(string department, int page = 1, int pageSize = 50)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .Where(c => c.Department.ToLower() == department.ToLower())
            .OrderBy(c => c.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAllDepartmentsAsync()
    {
        return await _context.Contacts
            .Where(c => !string.IsNullOrEmpty(c.Department))
            .Select(c => c.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }
}
