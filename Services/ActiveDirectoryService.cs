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
    private readonly string _container;

    public ActiveDirectoryService(IConfiguration configuration, ILogger<ActiveDirectoryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _domain = _configuration["ActiveDirectory:Domain"] ?? throw new ArgumentNullException("ActiveDirectory:Domain");
        _container = _configuration["ActiveDirectory:Container"] ?? throw new ArgumentNullException("ActiveDirectory:Container");
    }

    public bool ValidateCredentials(string username, string password)
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
            return context.ValidateCredentials(username, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке учетных данных AD");
            return false;
        }
    }

    public bool IsUserInGroup(string username, string groupName)
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
            using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
            
            if (user == null)
                return false;

            using var group = GroupPrincipal.FindByIdentity(context, IdentityType.Name, groupName);
            if (group == null)
                return false;

            return user.IsMemberOf(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке членства в группе AD");
            return false;
        }
    }

    public string GetUserDisplayName(string username)
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
            using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
            return user?.DisplayName ?? username;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении отображаемого имени пользователя AD");
            return username;
        }
    }
} 