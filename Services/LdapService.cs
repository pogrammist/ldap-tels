using ldap_tels.Data;
using ldap_tels.Models;
using Microsoft.EntityFrameworkCore;
using System.DirectoryServices.Protocols;
using System.Net;

namespace ldap_tels.Services;

public class LdapService : ILdapService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LdapService> _logger;

    public LdapService(ApplicationDbContext context, ILogger<LdapService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<LdapSource>> GetAllSourcesAsync()
    {
        return await _context.LdapSources.ToListAsync();
    }

    public async Task<LdapSource?> GetSourceByIdAsync(int id)
    {
        return await _context.LdapSources.FindAsync(id);
    }

    public async Task<LdapSource> AddSourceAsync(LdapSource source)
    {
        _context.LdapSources.Add(source);
        await _context.SaveChangesAsync();
        return source;
    }

    public async Task<bool> UpdateSourceAsync(LdapSource source)
    {
        try
        {
            var existingSource = await _context.LdapSources.FindAsync(source.Id);
            if (existingSource == null)
            {
                return false;
            }

            // Если пароль пустой, сохраняем старый пароль
            if (string.IsNullOrEmpty(source.BindPassword))
            {
                source.BindPassword = existingSource.BindPassword;
            }

            _context.Entry(existingSource).State = EntityState.Detached;
            _context.Entry(source).State = EntityState.Modified;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.LdapSources.AnyAsync(e => e.Id == source.Id))
            {
                return false;
            }
            throw;
        }
    }

    public async Task<bool> DeleteSourceAsync(int id)
    {
        var source = await _context.LdapSources.FindAsync(id);
        if (source == null)
        {
            return false;
        }

        _context.LdapSources.Remove(source);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task SyncAllSourcesAsync()
    {
        var sources = await _context.LdapSources.Where(s => s.IsActive).ToListAsync();
        foreach (var source in sources)
        {
            await SyncSourceAsync(source);
        }
    }

    public async Task SyncSourceAsync(LdapSource source)
    {
        try
        {
            _logger.LogInformation("Начало синхронизации с LDAP-сервером: {Name}", source.Name);

            // Получаем контакты из LDAP
            var ldapContacts = GetContactsFromLdap(source);
            var ldapDns = ldapContacts.Select(c => c.DistinguishedName).ToHashSet();

            // Получаем все LDAP-контакты из базы для этого источника
            var dbContacts = await _context.LdapContacts
                .Where(c => c.LdapSourceId == source.Id)
                .ToListAsync();
            var dbContactsByDn = dbContacts
                .Where(c => !string.IsNullOrEmpty(c.DistinguishedName))
                .ToDictionary(c => c.DistinguishedName, c => c);

            // Собираем все уникальные названия для batch processing
            var allDivisionNames = ldapContacts
                .Select(c => c.Division?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            var allDepartmentNames = ldapContacts
                .Select(c => c.Department?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            var allTitleNames = ldapContacts
                .Select(c => c.Title?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            var allCompanyNames = ldapContacts
                .Select(c => c.Company?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            // Получаем существующие сущности одним запросом
            var existingDivisions = await _context.Divisions
                .Where(d => allDivisionNames.Contains(d.Name))
                .ToDictionaryAsync(d => d.Name);

            var existingDepartments = await _context.Departments
                .Where(d => allDepartmentNames.Contains(d.Name))
                .ToDictionaryAsync(d => d.Name);

            var existingTitles = await _context.Titles
                .Where(t => allTitleNames.Contains(t.Name))
                .ToDictionaryAsync(t => t.Name);

            var existingCompanies = await _context.Companies
                .Where(c => allCompanyNames.Contains(c.Name))
                .ToDictionaryAsync(c => c.Name);

            // Создаем новые сущности, которых нет в базе
            foreach (var divisionName in allDivisionNames)
            {
                if (!existingDivisions.ContainsKey(divisionName))
                {
                    var newDivision = new Division { Name = divisionName };
                    _context.Divisions.Add(newDivision);
                    existingDivisions[divisionName] = newDivision;
                }
            }

            foreach (var departmentName in allDepartmentNames)
            {
                if (!existingDepartments.ContainsKey(departmentName))
                {
                    var newDepartment = new Department { Name = departmentName };
                    _context.Departments.Add(newDepartment);
                    existingDepartments[departmentName] = newDepartment;
                }
            }

            foreach (var titleName in allTitleNames)
            {
                if (!existingTitles.ContainsKey(titleName))
                {
                    var newTitle = new Title { Name = titleName };
                    _context.Titles.Add(newTitle);
                    existingTitles[titleName] = newTitle;
                }
            }

            foreach (var companyName in allCompanyNames)
            {
                if (!existingCompanies.ContainsKey(companyName))
                {
                    var newCompany = new Company { Name = companyName };
                    _context.Companies.Add(newCompany);
                    existingCompanies[companyName] = newCompany;
                }
            }

            // Сохраняем все новые сущности одним вызовом
            await _context.SaveChangesAsync();

            // 1. Удаляем устаревшие контакты (только для этого источника)
            var toDelete = dbContacts.Where(c => !ldapDns.Contains(c.DistinguishedName)).ToList();
            if (toDelete.Count > 0)
            {
                _context.LdapContacts.RemoveRange(toDelete);
                _logger.LogInformation($"Удалено {toDelete.Count} устаревших контактов для источника {source.Name}");
            }

            // 2. Обновляем существующие и добавляем новые (только для этого источника)
            foreach (var ldapContact in ldapContacts)
            {
                // Извлекаем строковые значения из навигационных свойств
                string? divisionName = ldapContact.Division?.Name;
                string? departmentName = ldapContact.Department?.Name;
                string? titleName = ldapContact.Title?.Name;
                string? companyName = ldapContact.Company?.Name;

                // Получаем ID сущностей
                int? divisionId = !string.IsNullOrEmpty(divisionName) && existingDivisions.TryGetValue(divisionName, out var division) 
                    ? division.Id 
                    : null;

                int? departmentId = !string.IsNullOrEmpty(departmentName) && existingDepartments.TryGetValue(departmentName, out var department) 
                    ? department.Id 
                    : null;

                int? titleId = !string.IsNullOrEmpty(titleName) && existingTitles.TryGetValue(titleName, out var title) 
                    ? title.Id 
                    : null;

                int? companyId = !string.IsNullOrEmpty(companyName) && existingCompanies.TryGetValue(companyName, out var company) 
                    ? company.Id 
                    : null;

                if (dbContactsByDn.TryGetValue(ldapContact.DistinguishedName, out var dbContact))
                {
                    // Обновляем существующий контакт
                    dbContact.DisplayName = ldapContact.DisplayName;
                    dbContact.Email = ldapContact.Email;
                    dbContact.PhoneNumber = ldapContact.PhoneNumber;
                    dbContact.DivisionId = divisionId;
                    dbContact.DepartmentId = departmentId;
                    dbContact.TitleId = titleId;
                    dbContact.CompanyId = companyId;
                    dbContact.LastUpdated = DateTime.UtcNow;
                    _context.LdapContacts.Update(dbContact);
                }
                else
                {
                    // Добавляем новый контакт
                    var newContact = new LdapContact
                    {
                        DistinguishedName = ldapContact.DistinguishedName,
                        DisplayName = ldapContact.DisplayName,
                        Email = ldapContact.Email,
                        PhoneNumber = ldapContact.PhoneNumber,
                        DivisionId = divisionId,
                        DepartmentId = departmentId,
                        TitleId = titleId,
                        CompanyId = companyId,
                        LdapSourceId = source.Id,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.LdapContacts.Add(newContact);
                }
            }

            // Обновляем время последней синхронизации
            source.LastSyncTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Синхронизация с LDAP-сервером {Name} завершена. Получено {Count} контактов",
                source.Name, ldapContacts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при синхронизации с LDAP-сервером {Name}", source.Name);
            throw;
        }
    }

    // Получение контактов из LDAP (только fetch, без работы с базой)
    // Оставляем virtual для подмены в тестах без внедрения отдельного фетчера
    protected virtual List<LdapContact> GetContactsFromLdap(LdapSource source)
    {
        var contacts = new List<LdapContact>();

        // Создаем идентификатор сервера LDAP
        var ldapIdentifier = new LdapDirectoryIdentifier(source.Server, source.Port, false, false);

        // Создаем учетные данные для подключения
        NetworkCredential? credentials = null;
        if (!string.IsNullOrEmpty(source.BindDn) && !string.IsNullOrEmpty(source.BindPassword))
        {
            credentials = new NetworkCredential(source.BindDn, source.BindPassword);
        }

        // Создаем подключение к LDAP серверу
        using var connection = new LdapConnection(ldapIdentifier, credentials);

        try
        {
            // Настройка SSL, если требуется
            if (source.UseSSL)
            {
                connection.SessionOptions.SecureSocketLayer = true;
                connection.SessionOptions.VerifyServerCertificate = (conn, cert) => true; // Отключаем проверку сертификата для тестирования
            }

            // Устанавливаем таймаут
            connection.Timeout = TimeSpan.FromSeconds(30);

            // Подключаемся к серверу
            connection.Bind();

            // Создаем запрос поиска
            var searchRequest = new SearchRequest(
                source.BaseDn,
                source.SearchFilter,
                System.DirectoryServices.Protocols.SearchScope.Subtree,
                "cn", "displayName", "mail", "telephoneNumber", "description", "department", "title", "company"
            );

            // Выполняем поиск
            var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

            // Обрабатываем результаты
            foreach (SearchResultEntry entry in searchResponse.Entries)
            {
                var contact = new LdapContact
                {
                    DistinguishedName = entry.DistinguishedName,
                    LdapSourceId = source.Id,
                    LdapSource = source,
                    LastUpdated = DateTime.UtcNow
                };

                // Заполнение полей контакта из атрибутов LDAP
                if (entry.Attributes.Contains("displayName"))
                {
                    var displayName = entry.Attributes["displayName"][0] as string ?? string.Empty;
                    if (string.IsNullOrEmpty(displayName))
                    {
                        if (entry.Attributes.Contains("cn"))
                        {
                            displayName = entry.Attributes["cn"][0] as string ?? string.Empty;
                        }
                    }
                    contact.DisplayName = displayName;
                }

                if (entry.Attributes.Contains("mail"))
                {
                    contact.Email = entry.Attributes["mail"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("telephoneNumber"))
                {
                    contact.PhoneNumber = entry.Attributes["telephoneNumber"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("description"))
                {
                    var divisionName = entry.Attributes["description"][0] as string ?? string.Empty;
                    if (!string.IsNullOrEmpty(divisionName))
                    {
                        contact.Division = new Division { Name = divisionName };
                    }
                }

                if (entry.Attributes.Contains("department"))
                {
                    var departmentName = entry.Attributes["department"][0] as string ?? string.Empty;
                    if (!string.IsNullOrEmpty(departmentName))
                    {
                        contact.Department = new Department { Name = departmentName };
                    }
                }

                if (entry.Attributes.Contains("title"))
                {
                    var titleName = entry.Attributes["title"][0] as string ?? string.Empty;
                    if (!string.IsNullOrEmpty(titleName))
                    {
                        contact.Title = new Title { Name = titleName };
                    }
                }

                if (entry.Attributes.Contains("company"))
                {
                    var companyName = entry.Attributes["company"][0] as string ?? string.Empty;
                    if (!string.IsNullOrEmpty(companyName))
                    {
                        contact.Company = new Company { Name = companyName };
                    }
                }

                contacts.Add(contact);
            }

            return contacts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении контактов из LDAP: {Message}", ex.Message);
            throw;
        }
    }
}
