using ad_tels.Data;
using ad_tels.Models;
using Microsoft.EntityFrameworkCore;
using System.DirectoryServices.Protocols;
using System.Net;

namespace ad_tels.Services;

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
            
            var contacts = await GetContactsFromLdapAsync(source);
            
            // Получаем список DN всех контактов из LDAP
            var ldapDns = contacts.Select(c => c.DistinguishedName).ToList();
            
            // Находим контакты, которых нет в LDAP
            var contactsToDelete = await _context.Contacts
                .Where(c => c.LdapSourceId == source.Id && !ldapDns.Contains(c.DistinguishedName))
                .ToListAsync();
            
            // Удаляем контакты, которых нет в LDAP
            if (contactsToDelete.Any())
            {
                _context.Contacts.RemoveRange(contactsToDelete);
                _logger.LogInformation("Удалено {Count} контактов, которых нет в LDAP", contactsToDelete.Count);
            }
            
            // Обновляем время последней синхронизации
            source.LastSyncTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Синхронизация с LDAP-сервером {Name} завершена. Получено {Count} контактов", 
                source.Name, contacts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при синхронизации с LDAP-сервером {Name}", source.Name);
            throw;
        }
    }

    private async Task<List<Contact>> GetContactsFromLdapAsync(LdapSource source)
    {
        var contacts = new List<Contact>();
        
        // Создаем идентификатор сервера LDAP
        var ldapIdentifier = new LdapDirectoryIdentifier(source.Server, source.Port, false, false);
        
        // Создаем учетные данные для подключения
        NetworkCredential credentials = null;
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
                "cn", "sn", "givenName", "mail", "telephoneNumber", "mobile", 
                "department", "title", "company", "displayName"
            );
            
            // Выполняем поиск
            var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
            
            // Обрабатываем результаты
            foreach (SearchResultEntry entry in searchResponse.Entries)
            {
                var contact = new Contact
                {
                    LdapSourceId = source.Id,
                    DistinguishedName = entry.DistinguishedName,
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
                
                if (entry.Attributes.Contains("mobile"))
                {
                    contact.Mobile = entry.Attributes["mobile"][0] as string ?? string.Empty;
                }
                
                if (entry.Attributes.Contains("department"))
                {
                    contact.Department = entry.Attributes["department"][0] as string ?? string.Empty;
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
                
                // Проверяем, существует ли уже контакт с таким DN
                var existingContact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.DistinguishedName == contact.DistinguishedName);
                
                if (existingContact != null)
                {
                    // Обновляем существующий контакт
                    existingContact.DisplayName = contact.DisplayName;
                    existingContact.FirstName = contact.FirstName;
                    existingContact.LastName = contact.LastName;
                    existingContact.Email = contact.Email;
                    existingContact.PhoneNumber = contact.PhoneNumber;
                    existingContact.Mobile = contact.Mobile;
                    existingContact.Department = contact.Department;
                    existingContact.Title = contact.Title;
                    existingContact.Company = contact.Company;
                    existingContact.LastUpdated = DateTime.UtcNow;
                    
                    _context.Contacts.Update(existingContact);
                }
                else
                {
                    // Добавляем новый контакт
                    _context.Contacts.Add(contact);
                    contacts.Add(contact);
                }
            }
            
            // Сохраняем изменения в базе данных
            await _context.SaveChangesAsync();
            
            return contacts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении контактов из LDAP: {Message}", ex.Message);
            throw;
        }
    }
}
