using ldap_tels.Data;
using ldap_tels.Models;
using Microsoft.EntityFrameworkCore;
using System.DirectoryServices.Protocols;
using System.Net;

namespace ldap_tels.Services;

public class LdapService
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
            var ldapContacts = GetContactsFromLdapAsync(source);
            var ldapDns = ldapContacts.Select(c => c.DistinguishedName).ToHashSet();

            // Получаем все LDAP-контакты из базы для этого источника
            var dbContacts = await _context.LdapContacts
                .Where(c => c.LdapSourceId == source.Id)
                .ToListAsync();
            var dbContactsByDn = dbContacts
                .Where(c => !string.IsNullOrEmpty(c.DistinguishedName))
                .ToDictionary(c => c.DistinguishedName, c => c);

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

                // --- Division ---
                Division? division = null;
                if (!string.IsNullOrWhiteSpace(divisionName))
                {
                    division = await _context.Divisions.FirstOrDefaultAsync(d => d.Name == divisionName);
                    if (division == null)
                    {
                        division = new Division { Name = divisionName };
                        _context.Divisions.Add(division);
                        await _context.SaveChangesAsync();
                    }
                }

                // --- Department ---
                Department? department = null;
                if (!string.IsNullOrWhiteSpace(departmentName))
                {
                    department = await _context.Departments.FirstOrDefaultAsync(d => d.Name == departmentName);
                    if (department == null)
                    {
                        department = new Department { Name = departmentName };
                        _context.Departments.Add(department);
                        await _context.SaveChangesAsync();
                    }
                }

                // --- Title ---
                Title? title = null;
                if (!string.IsNullOrWhiteSpace(titleName))
                {
                    title = await _context.Titles.FirstOrDefaultAsync(t => t.Name == titleName);
                    if (title == null)
                    {
                        title = new Title { Name = titleName };
                        _context.Titles.Add(title);
                        await _context.SaveChangesAsync();
                    }
                }

                // --- Company ---
                Company? company = null;
                if (!string.IsNullOrWhiteSpace(companyName))
                {
                    company = await _context.Companies.FirstOrDefaultAsync(c => c.Name == companyName);
                    if (company == null)
                    {
                        company = new Company { Name = companyName };
                        _context.Companies.Add(company);
                        await _context.SaveChangesAsync();
                    }
                }

                if (dbContactsByDn.TryGetValue(ldapContact.DistinguishedName, out var dbContact))
                {
                    // Обновляем существующий контакт
                    dbContact.DisplayName = ldapContact.DisplayName;
                    dbContact.Email = ldapContact.Email;
                    dbContact.PhoneNumber = ldapContact.PhoneNumber;
                    dbContact.DivisionId = division?.Id;
                    dbContact.DepartmentId = department?.Id;
                    dbContact.TitleId = title?.Id;
                    dbContact.CompanyId = company?.Id;
                    dbContact.LdapSourceId = source.Id;
                    dbContact.LdapSource = source;
                    dbContact.DistinguishedName = ldapContact.DistinguishedName;
                    dbContact.LastUpdated = DateTime.UtcNow;
                    _context.LdapContacts.Update(dbContact);
                }
                else
                {
                    // Добавляем новый контакт
                    ldapContact.DivisionId = division?.Id;
                    ldapContact.DepartmentId = department?.Id;
                    ldapContact.TitleId = title?.Id;
                    ldapContact.CompanyId = company?.Id;
                    ldapContact.LdapSourceId = source.Id;
                    ldapContact.LdapSource = source;
                    ldapContact.LastUpdated = DateTime.UtcNow;
                    _context.LdapContacts.Add(ldapContact);
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
    private List<LdapContact> GetContactsFromLdapAsync(LdapSource source)
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
