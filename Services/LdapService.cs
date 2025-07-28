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

            // Получаем все контакты из базы для этого источника (только Ldap контакты)
            var dbContacts = await _context.Contacts
                .Where(c => c.LdapSourceId == source.Id)
                .ToListAsync();
            var dbContactsByDn = dbContacts
                .Where(c => !string.IsNullOrEmpty(c.DistinguishedName))
                .ToDictionary(c => c.DistinguishedName!, c => c);

            // 1. Удаляем устаревшие контакты (только для этого источника)
            var toDelete = dbContacts.Where(c => !ldapDns.Contains(c.DistinguishedName)).ToList();
            if (toDelete.Count > 0)
            {
                _context.Contacts.RemoveRange(toDelete);
                _logger.LogInformation($"Удалено {toDelete.Count} устаревших контактов для источника {source.Name}");
            }

            // 2. Обновляем существующие и добавляем новые (только для этого источника)
            foreach (var ldapContact in ldapContacts)
            {
                if (dbContactsByDn.TryGetValue(ldapContact.DistinguishedName!, out var dbContact))
                {
                    // Обновляем существующий контакт
                    dbContact.DisplayName = ldapContact.DisplayName;
                    dbContact.FirstName = ldapContact.FirstName;
                    dbContact.LastName = ldapContact.LastName;
                    dbContact.Email = ldapContact.Email;
                    dbContact.PhoneNumber = ldapContact.PhoneNumber;
                    dbContact.Department = ldapContact.Department;
                    dbContact.Division = ldapContact.Division;
                    dbContact.Title = ldapContact.Title;
                    dbContact.Company = ldapContact.Company;                    
                    dbContact.LdapSourceId = source.Id;
                    dbContact.LdapSource = source;
                    dbContact.ContactType = ContactType.Ldap;
                    dbContact.LastUpdated = DateTime.UtcNow;
                    _context.Contacts.Update(dbContact);
                }
                else
                {
                    // Добавляем новый контакт
                    ldapContact.LdapSourceId = source.Id;
                    ldapContact.LdapSource = source;
                    ldapContact.ContactType = ContactType.Ldap;
                    ldapContact.LastUpdated = DateTime.UtcNow;
                    _context.Contacts.Add(ldapContact);
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
    private List<Contact> GetContactsFromLdapAsync(LdapSource source)
    {
        var contacts = new List<Contact>();

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
                "cn", "sn", "givenName", "mail", "telephoneNumber", "Description",
                "department", "title", "company", "displayName"
            );

            // Выполняем поиск
            var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

            // Обрабатываем результаты
            foreach (SearchResultEntry entry in searchResponse.Entries)
            {
                var contact = new Contact
                {
                    DistinguishedName = entry.DistinguishedName,
                    LdapSourceId = source.Id,
                    LdapSource = source,
                    ContactType = ContactType.Ldap,
                    LastUpdated = DateTime.UtcNow
                };

                // Заполнение полей контакта из атрибутов LDAP
                if (entry.Attributes.Contains("displayName"))
                {
                    contact.DisplayName = entry.Attributes["displayName"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("givenName"))
                {
                    contact.FirstName = entry.Attributes["givenName"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("sn"))
                {
                    contact.LastName = entry.Attributes["sn"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("mail"))
                {
                    contact.Email = entry.Attributes["mail"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("telephoneNumber"))
                {
                    contact.PhoneNumber = entry.Attributes["telephoneNumber"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("department"))
                {
                    contact.Department = entry.Attributes["department"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("Description"))
                {
                    contact.Division = entry.Attributes["Description"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("title"))
                {
                    contact.Title = entry.Attributes["title"][0] as string ?? string.Empty;
                }

                if (entry.Attributes.Contains("company"))
                {
                    contact.Company = entry.Attributes["company"][0] as string ?? string.Empty;
                }

                // Если DisplayName пустой, создаем его из имени и фамилии
                if (string.IsNullOrEmpty(contact.DisplayName) &&
                    (!string.IsNullOrEmpty(contact.FirstName) || !string.IsNullOrEmpty(contact.LastName)))
                {
                    contact.DisplayName = $"{contact.FirstName} {contact.LastName}".Trim();
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
