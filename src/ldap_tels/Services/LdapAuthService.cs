using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using ldap_tels.Configuration;

namespace ldap_tels.Services;

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
            using var connection = new LdapConnection();
            connection.SecureSocketLayer = _settings.UseSsl;
            await connection.ConnectAsync(_settings.Server, _settings.Port);

            var userDn = $"{username}@{_settings.Domain}";
            _logger.LogDebug("Подключение к LDAP серверу {Server}:{Port} с DN: {UserDn}",
                _settings.Server, _settings.Port, userDn);

            await connection.BindAsync(userDn, password);

            _logger.LogInformation("Пользователь {Username} успешно аутентифицирован", username);
            return connection.Bound;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Ошибка аутентификации для пользователя {Username}. Код ошибки: {ErrorCode}, Сообщение: {Message}",
                username, ex.ResultCode, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при аутентификации пользователя {Username}", username);
            return false;
        }
    }

    public async Task<bool> IsUserInAdminGroup(string username)
    {
        _logger.LogInformation("Проверка прав администратора для пользователя {Username}", username);
        try
        {
            using var connection = new LdapConnection();
            connection.SecureSocketLayer = _settings.UseSsl;
            await connection.ConnectAsync(_settings.Server, _settings.Port);

            _logger.LogDebug("Подключение к LDAP серверу с учетными данными сервиса");
            var bindUsername = $"{_settings.BindUsername}@{_settings.Domain}";
            await connection.BindAsync(bindUsername, _settings.BindPassword);

            var searchFilter = $"(&(objectClass=user)(sAMAccountName={username}))";
            _logger.LogDebug("Поиск пользователя с фильтром: {Filter}", searchFilter);

            var searchResults = await connection.SearchAsync(
                _settings.SearchBase,
                LdapConnection.ScopeSub,
                searchFilter,
                new[] { "memberOf" },
                false
            );

            await foreach (var entry in searchResults)
            {
                var memberOf = entry.Get("memberOf");
                if (memberOf != null)
                {
                    var groups = memberOf.StringValueArray;
                    _logger.LogDebug("Найдены группы пользователя: {Groups}", string.Join(", ", groups));

                    var isAdmin = groups.Any(g => g.Contains(_settings.AdminGroupDn));
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
                username, ex.ResultCode, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при проверке прав администратора для пользователя {Username}", username);
            return false;
        }
    }

    public async Task<string> GetUserDisplayName(string username)
    {
        _logger.LogInformation("Получение отображаемого имени для пользователя {Username}", username);
        try
        {
            using var connection = new LdapConnection();
            connection.SecureSocketLayer = _settings.UseSsl;
            await connection.ConnectAsync(_settings.Server, _settings.Port);

            _logger.LogDebug("Подключение к LDAP серверу с учетными данными сервиса");
            await connection.BindAsync(_settings.BindUsername, _settings.BindPassword);

            var searchFilter = $"(&(objectClass=user)(sAMAccountName={username}))";
            _logger.LogDebug("Поиск пользователя с фильтром: {Filter}", searchFilter);

            var searchResults = await connection.SearchAsync(
                _settings.SearchBase,
                LdapConnection.ScopeSub,
                searchFilter,
                new[] { "displayName" },
                false
            );

            await foreach (var entry in searchResults)
            {
                var displayName = entry.Get("displayName");
                if (displayName != null)
                {
                    var name = displayName.StringValue ?? username;
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
                username, ex.ResultCode, ex.Message);
            return username;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при получении отображаемого имени для пользователя {Username}", username);
            return username;
        }
    }
}