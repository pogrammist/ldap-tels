using ldap_tels.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ldap_tels.Services;

public class DevAuthService : ILdapAuthService
{
	private readonly ILogger<DevAuthService> _logger;

	public DevAuthService(ILogger<DevAuthService> logger)
	{
		_logger = logger;
	}

	public Task<bool> ValidateCredentialsAsync(string username, string password)
	{
		_logger.LogInformation("Dev-режим: попытка входа пользователя {Username}", username);

		// В dev-режиме разрешаем вход с любыми учетными данными
		return Task.FromResult(true);
	}

	public Task<bool> IsUserInAdminGroup(string username)
	{
		_logger.LogInformation("Dev-режим: проверка прав администратора для пользователя {Username}", username);

		// В dev-режиме все пользователи являются администраторами
		return Task.FromResult(true);
	}

	public Task<string> GetUserDisplayName(string username)
	{
		_logger.LogInformation("Dev-режим: получение отображаемого имени для пользователя {Username}", username);

		// В dev-режиме используем имя пользователя как отображаемое имя
		return Task.FromResult(username);
	}
}