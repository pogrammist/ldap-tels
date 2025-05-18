using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Configuration;
using System.Runtime.Versioning;

namespace ad_tels.Services;

[SupportedOSPlatform("windows")]
public class ActiveDirectoryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ActiveDirectoryService> _logger;
    private readonly string _domain;
    private readonly string? _serverIP;
    private readonly string _container;

    public ActiveDirectoryService(IConfiguration configuration, ILogger<ActiveDirectoryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _domain = _configuration["ActiveDirectory:Domain"] ?? throw new ArgumentNullException("ActiveDirectory:Domain");
        _serverIP = _configuration["ActiveDirectory:ServerIP"];
        _container = _configuration["ActiveDirectory:Container"] ?? throw new ArgumentNullException("ActiveDirectory:Container");
    }

    public bool ValidateCredentials(string username, string password)
    {
        try
        {
            PrincipalContext context;
            if (!string.IsNullOrEmpty(_serverIP))
            {
                // Используем IP-адрес сервера, если он указан
                context = new PrincipalContext(ContextType.Domain, _serverIP, _container);
            }
            else
            {
                // Используем доменное имя, если IP-адрес не указан
                context = new PrincipalContext(ContextType.Domain, _domain, _container);
            }

            using (context)
            {
                return context.ValidateCredentials(username, password);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке учетных данных AD");
            return false;
        }
    }

    public bool IsUserInGroup(string username, string groupName)
    {
        return true;

        try
        {
            _logger.LogInformation("Проверка пользователя {Username} в группе {GroupName}", username, groupName);
            
            PrincipalContext context;
            if (!string.IsNullOrEmpty(_serverIP))
            {
                _logger.LogInformation("Используем IP-адрес сервера: {ServerIP}", _serverIP);
                context = new PrincipalContext(ContextType.Domain, _serverIP, _container);
            }
            else
            {
                _logger.LogInformation("Используем доменное имя: {Domain}", _domain);
                context = new PrincipalContext(ContextType.Domain, _domain, _container);
            }

            using (context)
            {
                _logger.LogInformation("Поиск пользователя {Username} в контексте", username);
                
                // Пробуем найти пользователя разными способами
                UserPrincipal? user = null;
                
                // 1. Пробуем найти по SamAccountName (короткое имя для входа)
                user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                
                // 2. Если не нашли, пробуем найти по UserPrincipalName (email-подобное имя)
                if (user == null)
                {
                    user = UserPrincipal.FindByIdentity(context, IdentityType.UserPrincipalName, $"{username}@{_domain}");
                }
                
                // 3. Если все еще не нашли, пробуем найти по DistinguishedName
                if (user == null)
                {
                    user = UserPrincipal.FindByIdentity(context, IdentityType.DistinguishedName, $"CN={username},CN=Users,{_container}");
                }
                
                if (user == null)
                {
                    _logger.LogWarning("Пользователь {Username} не найден ни одним из способов", username);
                    return false;
                }

                _logger.LogInformation("Пользователь найден: {DisplayName} (SamAccountName: {SamAccountName})", 
                    user.DisplayName, user.SamAccountName);
                
                _logger.LogInformation("Поиск группы {GroupName}", groupName);
                using var group = GroupPrincipal.FindByIdentity(context, IdentityType.Name, groupName);
                if (group == null)
                {
                    _logger.LogWarning("Группа {GroupName} не найдена", groupName);
                    return false;
                }

                _logger.LogInformation("Группа найдена: {GroupName}", group.Name);
                return user.IsMemberOf(group);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке членства в группе AD: {Message}", ex.Message);
            return false;
        }
    }

    public string GetUserDisplayName(string username)
    {
        try
        {
            PrincipalContext context;
            if (!string.IsNullOrEmpty(_serverIP))
            {
                // Используем IP-адрес сервера, если он указан
                context = new PrincipalContext(ContextType.Domain, _serverIP, _container);
            }
            else
            {
                // Используем доменное имя, если IP-адрес не указан
                context = new PrincipalContext(ContextType.Domain, _domain, _container);
            }

            using (context)
            {
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                return user?.DisplayName ?? username;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении отображаемого имени пользователя AD");
            return username;
        }
    }
}
