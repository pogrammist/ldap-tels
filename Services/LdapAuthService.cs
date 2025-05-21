using System.DirectoryServices.Protocols;
using System.Net;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ad_tels.Configuration;

namespace ad_tels.Services;

public class LdapAuthService : ILdapAuthService
{
    private readonly LdapSettings _settings;
    private readonly ILogger<LdapAuthService> _logger;

    public LdapAuthService(IOptions<LdapSettings> settings, ILogger<LdapAuthService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port));
            connection.AuthType = AuthType.Basic;
            var userDn = $"{_settings.Domain}\\{username}";

            await Task.Run(() => connection.Bind(new NetworkCredential(userDn, password)));
            return true;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Ошибка аутентификации для пользователя {Username}", username);
            return false;
        }
    }

    public bool IsUserInAdminGroup(string username)
    {
        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port));
            connection.AuthType = AuthType.Basic;
            connection.Bind(new NetworkCredential(_settings.BindUsername, _settings.BindPassword));

            var searchRequest = new SearchRequest(
                _settings.SearchBase,
                $"(&(objectClass=user)(sAMAccountName={username}))",
                SearchScope.Subtree,
                "memberOf"
            );

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            if (response.Entries.Count > 0)
            {
                var entry = response.Entries[0];
                var memberOf = entry.Attributes["memberOf"];
                if (memberOf != null)
                {
                    return memberOf.GetValues(typeof(string))
                        .Cast<string>()
                        .Any(value => value.Contains(_settings.AdminGroupDn));
                }
            }

            return false;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Ошибка при проверке группы администраторов для пользователя {Username}", username);
            return false;
        }
    }

    public string GetUserDisplayName(string username)
    {
        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port));
            connection.AuthType = AuthType.Basic;
            connection.Bind(new NetworkCredential(_settings.BindUsername, _settings.BindPassword));

            var searchRequest = new SearchRequest(
                _settings.SearchBase,
                $"(&(objectClass=user)(sAMAccountName={username}))",
                SearchScope.Subtree,
                "displayName"
            );

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            if (response.Entries.Count > 0)
            {
                var entry = response.Entries[0];
                var displayName = entry.Attributes["displayName"];
                if (displayName != null && displayName.Count > 0)
                {
                    return displayName[0].ToString() ?? username;
                }
            }

            return username;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Ошибка при получении отображаемого имени для пользователя {Username}", username);
            return username;
        }
    }
}