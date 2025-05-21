using System.DirectoryServices.Protocols;
using System.Net;
using ad_tels.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ad_tels.Services;

public interface ILdapAuthService
{
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<LdapUser> GetUserInfoAsync(string username);
    bool IsUserInAdminGroup(string username);
    string GetUserDisplayName(string username);
}

public class LdapUser
{
    public required string Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public List<string> Groups { get; set; } = new List<string>();
}

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
            _logger.LogInformation("Начало аутентификации пользователя {Username}", username);

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Пустые учетные данные для пользователя {Username}", username);
                return false;
            }

            // Формируем DN пользователя в формате CN=username,OU=Domain Users,DC=domain,DC=com
            var userDn = $"CN={username},OU=Domain Users,DC={_settings.Domain.Replace(".", ",DC=")}";
            _logger.LogDebug("Попытка аутентификации с DN: {UserDn}", userDn);

            return await Task.Run(() =>
            {
                using var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port))
                {
                    AuthType = AuthType.Basic,
                    SessionOptions =
                    {
                    ProtocolVersion = 3,
                    ReferralChasing = ReferralChasingOptions.None
                    },
                    Credential = new NetworkCredential(userDn, password)
                };

                try
                {
                    connection.Bind();
                    _logger.LogInformation("Успешная аутентификация пользователя {Username}", username);
                    return true;
                }
                catch (LdapException ex)
                {
                    _logger.LogWarning(ex, "Ошибка аутентификации пользователя {Username}: {Message}", username, ex.Message);
                    return false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Непредвиденная ошибка при аутентификации пользователя {Username}", username);
            return false;
        }
    }

    public async Task<LdapUser> GetUserInfoAsync(string username)
    {
        return await Task.Run(() =>
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port));
            connection.AuthType = AuthType.Basic;
            connection.SessionOptions.ProtocolVersion = 3;

            // Используем служебную учетную запись для поиска
            connection.Bind(new NetworkCredential(_settings.BindDn, _settings.BindPassword));

            var searchRequest = new SearchRequest(
                _settings.SearchBase,
                string.Format(_settings.UserSearchFilter, username),
                SearchScope.Subtree,
                new[] { "cn", "mail", "memberOf" }
            );

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            if (response.Entries.Count == 0)
            {
                throw new Exception($"Пользователь {username} не найден");
            }

            var entry = response.Entries[0];
            var user = new LdapUser
            {
                Username = username,
                DisplayName = entry.Attributes["cn"]?[0].ToString(),
                Email = entry.Attributes["mail"]?[0].ToString(),
                Groups = entry.Attributes["memberOf"]?.Cast<string>().ToList() ?? new List<string>()
            };

            return user;
        });
    }

    public bool IsUserInAdminGroup(string username)
    {
        try
        {
            using var connection = new LdapConnection(_settings.Server)
            {
                AuthType = AuthType.Basic,
                Credential = new NetworkCredential(_settings.BindDn, _settings.BindPassword)
            };

            var searchRequest = new SearchRequest(
                _settings.SearchBase,
                $"(sAMAccountName={username})",
                SearchScope.Subtree,
                new[] { "memberOf" }
            );

            var response = (SearchResponse)connection.SendRequest(searchRequest);
            if (response.Entries.Count == 0)
            {
                _logger.LogWarning("Пользователь {Username} не найден", username);
                return false;
            }

            var userEntry = response.Entries[0];
            var memberOf = userEntry.Attributes["memberOf"];
            if (memberOf == null)
            {
                _logger.LogWarning("У пользователя {Username} нет групп", username);
                return false;
            }

            var isAdmin = false;
            for (int i = 0; i < memberOf.Count; i++)
            {
                if (memberOf[i].ToString().Contains(_settings.AdminGroup))
                {
                    isAdmin = true;
                    break;
                }
            }

            _logger.LogInformation("Пользователь {Username} {IsAdmin} администратором",
                username, isAdmin ? "является" : "не является");
            return isAdmin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке прав администратора для пользователя {Username}", username);
            return false;
        }
    }

    public string GetUserDisplayName(string username)
    {
        try
        {
            using var connection = new LdapConnection(_settings.Server)
            {
                AuthType = AuthType.Basic,
                Credential = new NetworkCredential(_settings.BindDn, _settings.BindPassword)
            };

            var searchRequest = new SearchRequest(
                _settings.SearchBase,
                $"(sAMAccountName={username})",
                SearchScope.Subtree,
                new[] { "displayName" }
            );

            var response = (SearchResponse)connection.SendRequest(searchRequest);
            if (response.Entries.Count == 0)
            {
                _logger.LogWarning("Пользователь {Username} не найден", username);
                return username;
            }

            var displayNameAttr = response.Entries[0].Attributes["displayName"];
            string? displayName = null;
            if (displayNameAttr != null && displayNameAttr.Count > 0)
            {
                displayName = displayNameAttr[0].ToString();
            }
            return string.IsNullOrEmpty(displayName) ? username : displayName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении отображаемого имени для пользователя {Username}", username);
            return username;
        }
    }
}

