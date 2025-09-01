using ldap_tels.Data;
using ldap_tels.Models;
using ldap_tels.ViewModels;
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

    public async Task<int> GetTotalContactsCountAsync()
    {
        var manualCount = await _context.ManualContacts.CountAsync();
        var ldapCount = await _context.LdapContacts.CountAsync();
        return manualCount + ldapCount;
    }

    public async Task<int> GetTotalDivisionsCountAsync()
    {
        return await _context.Divisions.CountAsync();
    }

    public async Task<int> GetTotalDepartmentsCountAsync()
    {
        return await _context.Departments.CountAsync();
    }

    public async Task<int> GetTotalTitlesCountAsync()
    {
        return await _context.Titles.CountAsync();
    }

    public async Task<int> GetTotalCompaniesCountAsync()
    {
        return await _context.Companies.CountAsync();
    }

    public async Task<IEnumerable<string>> GetAllDivisionNamesAsync()
    {
        return await _context.Divisions
            .Select(c => c.Name)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAllDepartmentNamesAsync()
    {
        return await _context.Departments
            .Select(c => c.Name)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAllTitleNamesAsync()
    {
        return await _context.Titles
            .Select(c => c.Name)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAllCompanyNamesAsync()
    {
        return await _context.Contacts
            .Include(c => c.Company)
            .Where(c => c.Company != null)
            .Select(c => c.Company!.Name)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<IEnumerable<Division>> GetAllDivisionsAsync()
    {
        return await _context.Divisions
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
    {
        return await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Title>> GetAllTitlesAsync()
    {
        return await _context.Titles
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
    
    public async Task<int?> ResolveOrCreateDivisionIdAsync(string? divisionName)
    {
        if (string.IsNullOrWhiteSpace(divisionName)) return null;
        var name = divisionName.Trim();
        var existing = await _context.Divisions.FirstOrDefaultAsync(x => x.Name == name);
        if (existing != null) return existing.Id;
        var entity = new Division { Name = name };
        _context.Divisions.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<int?> ResolveOrCreateDepartmentIdAsync(string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName)) return null;
        var name = departmentName.Trim();
        var existing = await _context.Departments.FirstOrDefaultAsync(x => x.Name == name);
        if (existing != null) return existing.Id;
        var entity = new Department { Name = name };
        _context.Departments.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<int?> ResolveOrCreateTitleIdAsync(string? titleName)
    {
        if (string.IsNullOrWhiteSpace(titleName)) return null;
        var name = titleName.Trim();
        var existing = await _context.Titles.FirstOrDefaultAsync(x => x.Name == name);
        if (existing != null) return existing.Id;
        var entity = new Title { Name = name };
        _context.Titles.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<int?> ResolveOrCreateCompanyIdAsync(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName)) return null;
        var name = companyName.Trim();
        var existing = await _context.Companies.FirstOrDefaultAsync(x => x.Name == name);
        if (existing != null) return existing.Id;
        var entity = new Company { Name = name };
        _context.Companies.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<IEnumerable<ContactViewModel>> GetAllContactsAsync(int page = 1, int pageSize = 50)
    {
        // Выполняем запросы отдельно
        var manuals = await _context.ManualContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Manual,
                DistinguishedName = null,
                LdapSourceId = null,
                LdapSource = null,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        var ldaps = await _context.LdapContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Include(c => c.LdapSource)
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Ldap,
                DistinguishedName = c.DistinguishedName,
                LdapSourceId = c.LdapSourceId,
                LdapSource = c.LdapSource,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        // Объединяем и сортируем в памяти с учетом весов
        var allContacts = manuals.Concat(ldaps)
            .OrderBy(x => GetGroupOrder(x)) // Сначала по порядку групп
            .ThenByDescending(x => x.DivisionWeight) // Затем по весу подразделения (по убыванию)
            .ThenBy(x => x.Division ?? string.Empty) // Затем по названию подразделения
            .ThenByDescending(x => x.DepartmentWeight) // По весу отдела (по убыванию)
            .ThenBy(x => x.Department ?? string.Empty) // Затем по названию отдела
            .ThenByDescending(x => x.TitleWeight) // По весу должности (по убыванию)
            .ThenBy(x => x.Title ?? string.Empty) // Затем по названию должности
            .ThenBy(x => x.DisplayName) // И наконец по имени
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return allContacts;
    }
    public async Task<ManualContact?> GetManualContactByIdAsync(int id)
    {
        return await _context.ManualContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ContactViewModel?> GetContactByIdAsync(int id)
    {
        var manual = await _context.ManualContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (manual != null)
        {
            return new ContactViewModel
            {
                Id = manual.Id,
                DisplayName = manual.DisplayName,
                Email = manual.Email,
                PhoneNumber = manual.PhoneNumber,
                Division = manual.Division?.Name,
                Department = manual.Department?.Name,
                Title = manual.Title?.Name,
                Company = manual.Company?.Name,
                ContactType = ContactType.Manual,
                DivisionWeight = manual.Division?.Weight ?? 0,
                DepartmentWeight = manual.Department?.Weight ?? 0,
                TitleWeight = manual.Title?.Weight ?? 0
            };
        }
        var ldap = await _context.LdapContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Include(c => c.LdapSource)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (ldap != null)
        {
            return new ContactViewModel
            {
                Id = ldap.Id,
                DisplayName = ldap.DisplayName,
                Email = ldap.Email,
                PhoneNumber = ldap.PhoneNumber,
                Division = ldap.Division?.Name,
                Department = ldap.Department?.Name,
                Title = ldap.Title?.Name,
                Company = ldap.Company?.Name,
                ContactType = ContactType.Ldap,
                DistinguishedName = ldap.DistinguishedName,
                LdapSourceId = ldap.LdapSourceId,
                LdapSource = ldap.LdapSource,
                DivisionWeight = ldap.Division?.Weight ?? 0,
                DepartmentWeight = ldap.Department?.Weight ?? 0,
                TitleWeight = ldap.Title?.Weight ?? 0
            };
        }
        return null;
    }

    public async Task<IEnumerable<ContactViewModel>> SearchContactsAsync(string searchTerm, int page = 1, int pageSize = 50)
    {
        searchTerm = searchTerm.ToLower();

        // Выполняем запросы отдельно
        var manuals = await _context.ManualContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Where(c =>
                c.DisplayName.ToLower().Contains(searchTerm) ||
                c.Email.ToLower().Contains(searchTerm) ||
                c.PhoneNumber.Contains(searchTerm) ||
                (c.Division != null && c.Division.Name.ToLower().Contains(searchTerm)) ||
                (c.Department != null && c.Department.Name.ToLower().Contains(searchTerm)) ||
                (c.Title != null && c.Title.Name.ToLower().Contains(searchTerm)) ||
                (c.Company != null && c.Company.Name.ToLower().Contains(searchTerm))
            )
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Manual,
                DistinguishedName = null,
                LdapSourceId = null,
                LdapSource = null,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        var ldaps = await _context.LdapContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Include(c => c.LdapSource)
            .Where(c =>
                    c.DisplayName.ToLower().Contains(searchTerm) ||
                    c.Email.ToLower().Contains(searchTerm) ||
                    c.PhoneNumber.Contains(searchTerm) ||
                (c.Division != null && c.Division.Name.ToLower().Contains(searchTerm)) ||
                (c.Department != null && c.Department.Name.ToLower().Contains(searchTerm)) ||
                (c.Title != null && c.Title.Name.ToLower().Contains(searchTerm)) ||
                (c.Company != null && c.Company.Name.ToLower().Contains(searchTerm))
            )
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Ldap,
                DistinguishedName = c.DistinguishedName,
                LdapSourceId = c.LdapSourceId,
                LdapSource = c.LdapSource,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        // Объединяем и сортируем в памяти с учетом весов
        var allContacts = manuals.Concat(ldaps)
            .OrderBy(x => GetGroupOrder(x)) // Сначала по порядку групп
            .ThenByDescending(x => x.DivisionWeight) // Затем по весу подразделения (по убыванию)
            .ThenBy(x => x.Division ?? string.Empty) // Затем по названию подразделения
            .ThenByDescending(x => x.DepartmentWeight) // По весу отдела (по убыванию)
            .ThenBy(x => x.Department ?? string.Empty) // Затем по названию отдела
            .ThenByDescending(x => x.TitleWeight) // По весу должности (по убыванию)
            .ThenBy(x => x.Title ?? string.Empty) // Затем по названию должности
            .ThenBy(x => x.DisplayName) // И наконец по имени
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return allContacts;
    }

    public async Task<int> GetSearchResultsCountAsync(string searchTerm)
    {
        searchTerm = searchTerm.ToLower();
        var manualCount = await _context.ManualContacts
            .Where(c =>
                    c.DisplayName.ToLower().Contains(searchTerm) ||
                    c.Email.ToLower().Contains(searchTerm) ||
                    c.PhoneNumber.Contains(searchTerm) ||
                (c.Division != null && c.Division.Name.ToLower().Contains(searchTerm)) ||
                (c.Department != null && c.Department.Name.ToLower().Contains(searchTerm)) ||
                (c.Title != null && c.Title.Name.ToLower().Contains(searchTerm)) ||
                (c.Company != null && c.Company.Name.ToLower().Contains(searchTerm))
            )
            .CountAsync();
        var ldapCount = await _context.LdapContacts
            .Where(c =>
                c.DisplayName.ToLower().Contains(searchTerm) ||
                c.Email.ToLower().Contains(searchTerm) ||
                c.PhoneNumber.Contains(searchTerm) ||
                (c.Division != null && c.Division.Name.ToLower().Contains(searchTerm)) ||
                (c.Department != null && c.Department.Name.ToLower().Contains(searchTerm)) ||
                (c.Title != null && c.Title.Name.ToLower().Contains(searchTerm)) ||
                (c.Company != null && c.Company.Name.ToLower().Contains(searchTerm))
            )
            .CountAsync();
        return manualCount + ldapCount;
    }

    public async Task<IEnumerable<ContactViewModel>> GetContactsByDivisionAsync(string division, int page = 1, int pageSize = 50)
    {
        division = division.ToLower();
        
        // Выполняем запросы отдельно
        var manuals = await _context.ManualContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Where(c => c.Division != null && c.Division.Name.ToLower() == division)
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Manual,
                DistinguishedName = null,
                LdapSourceId = null,
                LdapSource = null,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        var ldaps = await _context.LdapContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Include(c => c.LdapSource)
            .Where(c => c.Division != null && c.Division.Name.ToLower() == division)
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Ldap,
                DistinguishedName = c.DistinguishedName,
                LdapSourceId = c.LdapSourceId,
                LdapSource = c.LdapSource,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        // Объединяем и сортируем в памяти с учетом весов
        var allContacts = manuals.Concat(ldaps)
            .OrderByDescending(x => x.DepartmentWeight) // По весу отдела (по убыванию)
            .ThenBy(x => x.Department ?? string.Empty) // Затем по названию отдела
            .ThenByDescending(x => x.TitleWeight) // По весу должности (по убыванию)
            .ThenBy(x => x.Title ?? string.Empty) // Затем по названию должности
            .ThenBy(x => x.DisplayName) // И наконец по имени
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return allContacts;
    }
    public async Task<int> GetContactsByDivisionCountAsync(string division)
    {
        division = division.ToLower();
        var manualCount = await _context.ManualContacts
            .Where(c => c.Division != null && c.Division.Name.ToLower() == division)
            .CountAsync();
        var ldapCount = await _context.LdapContacts
            .Where(c => c.Division != null && c.Division.Name.ToLower() == division)
            .CountAsync();
        return manualCount + ldapCount;
    }

    public async Task<IEnumerable<ContactViewModel>> GetContactsByDepartmentAsync(string department, int page = 1, int pageSize = 50)
    {
        department = department.ToLower();
        
        // Выполняем запросы отдельно
        var manuals = await _context.ManualContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Where(c => c.Department != null && c.Department.Name.ToLower() == department)
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Manual,
                DistinguishedName = null,
                LdapSourceId = null,
                LdapSource = null,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        var ldaps = await _context.LdapContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Include(c => c.LdapSource)
            .Where(c => c.Department != null && c.Department.Name.ToLower() == department)
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Ldap,
                DistinguishedName = c.DistinguishedName,
                LdapSourceId = c.LdapSourceId,
                LdapSource = c.LdapSource,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        // Объединяем и сортируем в памяти с учетом весов
        var allContacts = manuals.Concat(ldaps)
            .OrderByDescending(x => x.DivisionWeight) // По весу подразделения (по убыванию)
            .ThenBy(x => x.Division ?? string.Empty) // Затем по названию подразделения
            .ThenByDescending(x => x.TitleWeight) // По весу должности (по убыванию)
            .ThenBy(x => x.Title ?? string.Empty) // Затем по названию должности
            .ThenBy(x => x.DisplayName) // И наконец по имени
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return allContacts;
    }

    public async Task<int> GetContactsByDepartmentCountAsync(string department)
    {
        department = department.ToLower();
        var manualCount = await _context.ManualContacts
            .Where(c => c.Department != null && c.Department.Name.ToLower() == department)
            .CountAsync();
        var ldapCount = await _context.LdapContacts
            .Where(c => c.Department != null && c.Department.Name.ToLower() == department)
            .CountAsync();
        return manualCount + ldapCount;
    }

    public async Task<IEnumerable<ContactViewModel>> GetContactsByTitleAsync(string title, int page = 1, int pageSize = 50)
    {
        title = title.ToLower();
        
        // Выполняем запросы отдельно
        var manuals = await _context.ManualContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Where(c => c.Title != null && c.Title.Name.ToLower() == title)
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Manual,
                DistinguishedName = null,
                LdapSourceId = null,
                LdapSource = null,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        var ldaps = await _context.LdapContacts
            .Include(c => c.Division)
            .Include(c => c.Department)
            .Include(c => c.Title)
            .Include(c => c.Company)
            .Include(c => c.LdapSource)
            .Where(c => c.Title != null && c.Title.Name.ToLower() == title)
            .Select(c => new ContactViewModel
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Division = c.Division != null ? c.Division.Name : null,
                Department = c.Department != null ? c.Department.Name : null,
                Title = c.Title != null ? c.Title.Name : null,
                Company = c.Company != null ? c.Company.Name : null,
                ContactType = ContactType.Ldap,
                DistinguishedName = c.DistinguishedName,
                LdapSourceId = c.LdapSourceId,
                LdapSource = c.LdapSource,
                DivisionWeight = c.Division != null ? c.Division.Weight : 0,
                DepartmentWeight = c.Department != null ? c.Department.Weight : 0,
                TitleWeight = c.Title != null ? c.Title.Weight : 0
            })
            .ToListAsync();

        // Объединяем и сортируем в памяти с учетом весов
        var allContacts = manuals.Concat(ldaps)
            .OrderBy(x => GetGroupOrder(x)) // Сначала по порядку групп
            .ThenByDescending(x => x.DivisionWeight) // Затем по весу подразделения (по убыванию)
            .ThenBy(x => x.Division ?? string.Empty) // Затем по названию подразделения
            .ThenByDescending(x => x.DepartmentWeight) // По весу отдела (по убыванию)
            .ThenBy(x => x.Department ?? string.Empty) // Затем по названию отдела
            .ThenByDescending(x => x.TitleWeight) // По весу должности (по убыванию)
            .ThenBy(x => x.Title ?? string.Empty) // Затем по названию должности
            .ThenBy(x => x.DisplayName) // И наконец по имени
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return allContacts;
    }

    public async Task<int> GetContactsByTitleCountAsync(string title)
    {
        title = title.ToLower();
        var manualCount = await _context.ManualContacts
            .Where(c => c.Title != null && c.Title.Name.ToLower() == title)
            .CountAsync();
        var ldapCount = await _context.LdapContacts
            .Where(c => c.Title != null && c.Title.Name.ToLower() == title)
            .CountAsync();
        return manualCount + ldapCount;
    }

    public async Task AddContactAsync(ManualContact contact)
    {
        contact.LastUpdated = DateTime.UtcNow;
        _context.ManualContacts.Add(contact);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateContactAsync(ManualContact contact)
    {
        try
        {
            var existingContact = await _context.ManualContacts.FindAsync(contact.Id);
            if (existingContact == null)
            {
                return false;
            }
            // Обновляем только существующие в модели поля
            existingContact.DisplayName = contact.DisplayName;
            existingContact.Email = contact.Email;
            existingContact.PhoneNumber = contact.PhoneNumber;
            existingContact.DivisionId = contact.DivisionId;
            existingContact.DepartmentId = contact.DepartmentId;
            existingContact.TitleId = contact.TitleId;
            existingContact.CompanyId = contact.CompanyId;
            existingContact.LastUpdated = DateTime.UtcNow;
            _context.Entry(existingContact).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.ManualContacts.AnyAsync(e => e.Id == contact.Id))
            {
                return false;
            }
            throw;
        }
    }

    public async Task<bool> DeleteContactAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var contact = await _context.ManualContacts
                .Include(c => c.Division)
                .Include(c => c.Department)
                .Include(c => c.Title)
                .Include(c => c.Company)
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (contact == null)
            {
                return false;
            }

            // Сохраняем ID связанных entities перед удалением контакта
            var divisionId = contact.DivisionId;
            var departmentId = contact.DepartmentId;
            var titleId = contact.TitleId;
            var companyId = contact.CompanyId;

            // Удаляем контакт
            _context.ManualContacts.Remove(contact);
            await _context.SaveChangesAsync();

            // Проверяем и удаляем связанные entities если они больше не используются
            await CleanupUnusedDivisionAsync(divisionId);
            await CleanupUnusedDepartmentAsync(departmentId);
            await CleanupUnusedTitleAsync(titleId);
            await CleanupUnusedCompanyAsync(companyId);

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Ошибка при удалении контакта с ID {Id}", id);
            return false;
        }
    }

    private async Task CleanupUnusedDivisionAsync(int? divisionId)
    {
        if (divisionId.HasValue)
        {
            var isUsed = await _context.Contacts.AnyAsync(c => c.DivisionId == divisionId);
            if (!isUsed)
            {
                var division = await _context.Divisions.FindAsync(divisionId);
                if (division != null)
                {
                    _context.Divisions.Remove(division);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }

    private async Task CleanupUnusedDepartmentAsync(int? departmentId)
    {
        if (departmentId.HasValue)
        {
            var isUsed = await _context.Contacts.AnyAsync(c => c.DepartmentId == departmentId);
            if (!isUsed)
            {
                var department = await _context.Departments.FindAsync(departmentId);
                if (department != null)
                {
                    _context.Departments.Remove(department);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }

    private async Task CleanupUnusedTitleAsync(int? titleId)
    {
        if (titleId.HasValue)
        {
            var isUsed = await _context.Contacts.AnyAsync(c => c.TitleId == titleId);
            if (!isUsed)
            {
                var title = await _context.Titles.FindAsync(titleId);
                if (title != null)
                {
                    _context.Titles.Remove(title);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }

    private async Task CleanupUnusedCompanyAsync(int? companyId)
    {
        if (companyId.HasValue)
        {
            var isUsed = await _context.Contacts.AnyAsync(c => c.CompanyId == companyId);
            if (!isUsed)
            {
                var company = await _context.Companies.FindAsync(companyId);
                if (company != null)
                {
                    _context.Companies.Remove(company);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }

    public async Task<bool> UpdateDivisionWeightAsync(int divisionId, int delta)
    {
        var division = await _context.Divisions.FindAsync(divisionId);
        if (division == null)
        {
            return false;
        }

        // Изменяем вес с проверкой границ
        var newWeight = division.Weight + delta;
        newWeight = Math.Max(0, Math.Min(100, newWeight)); // Ограничение 0-100
        
        division.Weight = newWeight;
        await _context.SaveChangesAsync();
        
        return true;
    }

    // И метод для получения обновленного веса
    public async Task<int?> GetDivisionWeightAsync(int divisionId)
    {
        var division = await _context.Divisions.FindAsync(divisionId);
        return division?.Weight;
    }

    public async Task<bool> UpdateDepartmentWeightAsync(int departmentId, int delta)
    {
        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null)
        {
            return false;
        }

        // Изменяем вес с проверкой границ
        var newWeight = department.Weight + delta;
        newWeight = Math.Max(0, Math.Min(100, newWeight)); // Ограничение 0-100
        
        department.Weight = newWeight;
        await _context.SaveChangesAsync();
        
        return true;
    }

    // И метод для получения обновленного веса
    public async Task<int?> GetDepartmentWeightAsync(int departmentId)
    {
        var department = await _context.Departments.FindAsync(departmentId);
        return department?.Weight;
    }

    public async Task<bool> UpdateTitleWeightAsync(int titleId, int delta)
    {
        var title = await _context.Titles.FindAsync(titleId);
        if (title == null)
        {
            return false;
        }

        // Изменяем вес с проверкой границ
        var newWeight = title.Weight + delta;
        newWeight = Math.Max(0, Math.Min(100, newWeight)); // Ограничение 0-100
        
        title.Weight = newWeight;
        await _context.SaveChangesAsync();
        
        return true;
    }

    // И метод для получения обновленного веса
    public async Task<int?> GetTitleWeightAsync(int titleId)
    {
        var title = await _context.Titles.FindAsync(titleId);
        return title?.Weight;
    }

    /// <summary>
    /// Определяет порядок группы для сортировки
    /// 0 - группы с непустыми подразделениями (любые отделы)
    /// 1 - группы с пустыми подразделениями, но непустыми отделами
    /// 2 - группы с пустыми подразделениями и пустыми отделами
    /// </summary>
    private static int GetGroupOrder(ContactViewModel contact)
    {
        var hasDivision = !string.IsNullOrWhiteSpace(contact.Division);
        var hasDepartment = !string.IsNullOrWhiteSpace(contact.Department);

        if (hasDivision)
        {
            return 0; // Группы с непустыми подразделениями (любые отделы)
        }
        else if (hasDepartment)
        {
            return 1; // Группы с пустыми подразделениями, но непустыми отделами
        }
        else
        {
            return 2; // Группы с пустыми подразделениями и пустыми отделами
        }
    }
}
