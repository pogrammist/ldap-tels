using ldap_tels.Data;
using ldap_tels.Models;
using Microsoft.EntityFrameworkCore;

namespace ldap_tels.Services;

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
            .Where(c => c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
            .OrderBy(c => c.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalContactsCountAsync()
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .Where(c => c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
            .CountAsync();
    }

    public async Task<Contact?> GetContactByIdAsync(int id)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .FirstOrDefaultAsync(c => c.Id == id && (c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive)));
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
            .Where(c => (c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
                && (
                    c.DisplayName.ToLower().Contains(searchTerm) ||
                    c.FirstName.ToLower().Contains(searchTerm) ||
                    c.LastName.ToLower().Contains(searchTerm) ||
                    c.Email.ToLower().Contains(searchTerm) ||
                    c.PhoneNumber.Contains(searchTerm) ||
                    c.Department.ToLower().Contains(searchTerm) ||
                    c.Division.ToLower().Contains(searchTerm) ||
                    c.Title.ToLower().Contains(searchTerm) ||
                    c.Company.ToLower().Contains(searchTerm)
                )
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
            .Include(c => c.LdapSource)
            .Where(c => (c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
                && (
                    c.DisplayName.ToLower().Contains(searchTerm) ||
                    c.FirstName.ToLower().Contains(searchTerm) ||
                    c.LastName.ToLower().Contains(searchTerm) ||
                    c.Email.ToLower().Contains(searchTerm) ||
                    c.PhoneNumber.Contains(searchTerm) ||
                    c.Department.ToLower().Contains(searchTerm) ||
                    c.Division.ToLower().Contains(searchTerm) ||
                    c.Title.ToLower().Contains(searchTerm) ||
                    c.Company.ToLower().Contains(searchTerm)
                )
            )
            .CountAsync();
    }

    public async Task<IEnumerable<Contact>> GetContactsByDivisionAsync(string division, int page = 1, int pageSize = 50)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .Where(c => (c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
                && c.Division.ToLower() == division.ToLower())
            .OrderBy(c => c.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAllDivisionsAsync()
    {
        return await _context.Contacts
            .Where(c => !string.IsNullOrEmpty(c.Division))
            .Select(c => c.Division)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<int> GetContactsByDivisionCountAsync(string division)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .Where(c => (c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
                && c.Division.ToLower() == division.ToLower())
            .CountAsync();
    }

    public async Task<IEnumerable<Contact>> GetContactsByDepartmentAsync(string department, int page = 1, int pageSize = 50)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .Where(c => (c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
                && c.Department.ToLower() == department.ToLower())
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

    public async Task<int> GetContactsByDepartmentCountAsync(string department)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .Where(c => (c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
                && c.Department.ToLower() == department.ToLower())
            .CountAsync();
    }

    public async Task<IEnumerable<Contact>> GetContactsByTitleAsync(string title, int page = 1, int pageSize = 50)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .Where(c => (c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
                && c.Title.ToLower() == title.ToLower())
            .OrderBy(c => c.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAllTitlesAsync()
    {
        return await _context.Contacts
            .Where(c => !string.IsNullOrEmpty(c.Title))
            .Select(c => c.Title)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<int> GetContactsByTitleCountAsync(string title)
    {
        return await _context.Contacts
            .Include(c => c.LdapSource)
            .Where(c => (c.ContactType == ContactType.Manual || (c.ContactType == ContactType.Ldap && c.LdapSource != null && c.LdapSource.IsActive))
                && c.Title.ToLower() == title.ToLower())
            .CountAsync();
    }

    public async Task AddContactAsync(Contact contact)
    {
        if (contact.ContactType != ContactType.Manual)
            throw new InvalidOperationException("Only manual contacts can be created through this service.");
        contact.LastUpdated = DateTime.UtcNow;
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateContactAsync(Contact contact)
    {
        try
        {
            var existingContact = await _context.Contacts.FindAsync(contact.Id);
            if (existingContact == null)
            {
                return false;
            }
            if (existingContact.ContactType != ContactType.Manual || contact.ContactType != ContactType.Manual)
            {
                // Only manual contacts can be updated
                return false;
            }
            // Обновляем только существующие в модели поля
            existingContact.DisplayName = contact.DisplayName;
            existingContact.FirstName = contact.FirstName;
            existingContact.LastName = contact.LastName;
            existingContact.Email = contact.Email;
            existingContact.PhoneNumber = contact.PhoneNumber;
            existingContact.Division = contact.Division;
            existingContact.Department = contact.Department;
            existingContact.Title = contact.Title;
            existingContact.Company = contact.Company;
            existingContact.ContactType = contact.ContactType;
            existingContact.LastUpdated = DateTime.UtcNow;
            existingContact.DistinguishedName = null;
            existingContact.LdapSourceId = null;
            existingContact.LdapSource = null;
            _context.Entry(existingContact).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Contacts.AnyAsync(e => e.Id == contact.Id))
            {
                return false;
            }
            throw;
        }
    }

    public async Task<bool> DeleteContactAsync(int id)
    {
        try
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return false;
            }
            if (contact.ContactType != ContactType.Manual)
            {
                return false;
            }
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении контакта с ID {Id}", id);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetAllCompaniesAsync()
    {
        return await _context.Contacts
            .Where(c => !string.IsNullOrEmpty(c.Company))
            .Select(c => c.Company)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }
}
