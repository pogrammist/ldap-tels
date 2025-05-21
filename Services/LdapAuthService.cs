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
        _logger.LogInformation("LdapAuthService инициализирован с настройками: Server={Server}, Port={Port}, Domain={Domain}",
            _settings.Server, _settings.Port, _settings.Domain);
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        _logger.LogInformation("Начало аутентификации пользователя {Username}", username);
        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port));
            connection.AuthType = AuthType.Basic;
            var userDn = $"{_settings.Domain}\\{username}";

            _logger.LogDebug("Подключение к LDAP серверу {Server}:{Port}", _settings.Server, _settings.Port);
            await Task.Run(() => connection.Bind(new NetworkCredential(userDn, password)));

            _logger.LogInformation("Пользователь {Username} успешно аутентифицирован", username);
            return true;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Ошибка аутентификации для пользователя {Username}. Код ошибки: {ErrorCode}, Сообщение: {Message}",
                username, ex.ErrorCode, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при аутентификации пользователя {Username}", username);
            return false;
        }
    }

    public bool IsUserInAdminGroup(string username)
    {
        _logger.LogInformation("Проверка прав администратора для пользователя {Username}", username);
        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port));
            connection.AuthType = AuthType.Basic;

            _logger.LogDebug("Подключение к LDAP серверу с учетными данными сервиса");
            connection.Bind(new NetworkCredential(_settings.BindUsername, _settings.BindPassword));

            var searchFilter = $"(&(objectClass=user)(sAMAccountName={username}))";
            _logger.LogDebug("Поиск пользователя с фильтром: {Filter}", searchFilter);

            var searchRequest = new SearchRequest(
                _settings.SearchBase,
                searchFilter,
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
                    var groups = memberOf.GetValues(typeof(string)).Cast<string>().ToList();
                    _logger.LogDebug("Найдены группы пользователя: {Groups}", string.Join(", ", groups));

                    var isAdmin = groups.Any(value => value.Contains(_settings.AdminGroupDn));
                    _logger.LogInformation("Пользователь {Username} {IsAdmin} администратором",
                        username, isAdmin ? "является" : "не является");
                    return isAdmin;
                }
            }

            _logger.LogWarning("Пользователь {Username} не найден или не имеет групп", username);
            return false;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Ошибка при проверке группы администраторов для пользователя {Username}. Код ошибки: {ErrorCode}, Сообщение: {Message}",
                username, ex.ErrorCode, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при проверке прав администратора для пользователя {Username}", username);
            return false;
        }
    }

    public string GetUserDisplayName(string username)
    {
        _logger.LogInformation("Получение отображаемого имени для пользователя {Username}", username);
        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_settings.Server, _settings.Port));
            connection.AuthType = AuthType.Basic;

            _logger.LogDebug("Подключение к LDAP серверу с учетными данными сервиса");
            connection.Bind(new NetworkCredential(_settings.BindUsername, _settings.BindPassword));

            var searchFilter = $"(&(objectClass=user)(sAMAccountName={username}))";
            _logger.LogDebug("Поиск пользователя с фильтром: {Filter}", searchFilter);

            var searchRequest = new SearchRequest(
                _settings.SearchBase,
                searchFilter,
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
                    var name = displayName[0].ToString() ?? username;
                    _logger.LogInformation("Найдено отображаемое имя для пользователя {Username}: {DisplayName}", username, name);
                    return name;
                }
            }

            _logger.LogWarning("Отображаемое имя для пользователя {Username} не найдено, используется имя пользователя", username);
            return username;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Ошибка при получении отображаемого имени для пользователя {Username}. Код ошибки: {ErrorCode}, Сообщение: {Message}",
                username, ex.ErrorCode, ex.Message);
            return username;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при получении отображаемого имени для пользователя {Username}", username);
            return username;
        }
    }
}