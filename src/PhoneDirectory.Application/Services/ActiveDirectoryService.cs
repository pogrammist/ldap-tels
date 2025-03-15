using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LdapForNet;
using PhoneDirectory.Domain.Interfaces;
using PhoneDirectory.Domain.Entities;
using static LdapForNet.Native.Native;

namespace PhoneDirectory.Application.Services;

public class ActiveDirectoryService : IActiveDirectoryService
{
	private readonly ILogger<ActiveDirectoryService> _logger;
	private readonly IConfiguration _configuration;

	public ActiveDirectoryService(ILogger<ActiveDirectoryService> logger, IConfiguration configuration)
	{
		_logger = logger;
		_configuration = configuration;
	}

	public async Task<bool> TestConnectionAsync(DomainConnection domain)
	{
		try
		{
			await Task.Run(() =>
			{
				using var connection = new LdapConnection();
				connection.Connect(domain.Server, domain.Port);
				if (domain.UseSSL)
				{
					connection.SetOption(LdapOption.LDAP_OPT_SSL, 1);
				}
				connection.Bind(domain.Username, domain.Password);
			});
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error testing connection to {Server}", domain.Server);
			return false;
		}
	}

	public async Task<IEnumerable<Contact>> GetContactsAsync(DomainConnection domain)
	{
		try
		{
			return await Task.Run(() =>
			{
				using var connection = new LdapConnection();
				connection.Connect(domain.Server, domain.Port);
				if (domain.UseSSL)
				{
					connection.SetOption(LdapOption.LDAP_OPT_SSL, 1);
				}
				connection.Bind(domain.Username, domain.Password);

				var contacts = new List<Contact>();
				var entries = connection.Search(
					"DC=domain,DC=com", // базовый DN
					"(&(objectClass=user)(objectCategory=person))", // фильтр
					new[] { "displayName", "department", "title", "telephoneNumber", "mobile", "mail", "physicalDeliveryOfficeName" } // атрибуты
				);

				foreach (var entry in entries)
				{
					contacts.Add(new Contact
					{
						DisplayName = entry.DirectoryAttributes["displayName"]?.GetValue<string>() ?? "",
						Department = entry.DirectoryAttributes["department"]?.GetValue<string>() ?? "",
						Title = entry.DirectoryAttributes["title"]?.GetValue<string>() ?? "",
						Phone = entry.DirectoryAttributes["telephoneNumber"]?.GetValue<string>() ?? "",
						Mobile = entry.DirectoryAttributes["mobile"]?.GetValue<string>() ?? "",
						Email = entry.DirectoryAttributes["mail"]?.GetValue<string>() ?? "",
						Location = entry.DirectoryAttributes["physicalDeliveryOfficeName"]?.GetValue<string>() ?? "",
						DomainId = domain.Id
					});
				}

				return contacts;
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting contacts from {Server}", domain.Server);
			return Enumerable.Empty<Contact>();
		}
	}

	public async Task<bool> SyncContactsAsync(DomainConnection domain)
	{
		try
		{
			var contacts = await GetContactsAsync(domain);
			// Здесь должна быть логика синхронизации с базой данных
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error syncing contacts from {Server}", domain.Server);
			return false;
		}
	}
}
