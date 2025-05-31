using ldap_tels.Models;
using Microsoft.EntityFrameworkCore;

namespace ldap_tels.Data;

public static class DbInitializer
{
	public static async Task InitializeAsync(ApplicationDbContext context)
	{
		// Проверяем, есть ли уже данные в базе
		if (await context.LdapSources.AnyAsync())
		{
			return; // База данных уже инициализирована
		}

		// Добавляем тестовый LDAP-источник
		var testLdapSource = new LdapSource
		{
			Name = "Test LDAP Server",
			Server = "localhost",
			Port = 389,
			BaseDn = "dc=example,dc=com",
			BindDn = "cn=admin,dc=example,dc=com",
			BindPassword = "admin",
			SearchFilter = "(&(objectClass=person)(telephoneNumber=*))",
			UseSSL = false,
			IsActive = true,
			LastSyncTime = DateTime.UtcNow
		};

		context.LdapSources.Add(testLdapSource);
		await context.SaveChangesAsync();

		// Добавляем тестовые контакты
		var testContacts = new List<Contact>
		{
			new Contact
			{
				FirstName = "Ivan",
				LastName = "Ivanov",
				PhoneNumber = "+7 (999) 123-45-67",
				Department = "IT",
				Email = "ivanov@example.com",
				LdapSourceId = testLdapSource.Id,
				DisplayName = "Ivan Ivanov",
				Division = "Razrabotka",
				Title = "Starshiy razrabotchik",
				Company = "Example Corp",
				DistinguishedName = "cn=Ivan Ivanov,ou=IT,dc=example,dc=com",
				LastUpdated = DateTime.UtcNow
			},
			new Contact
			{
				FirstName = "Petr",
				LastName = "Petrov",
				PhoneNumber = "+7 (999) 765-43-21",
				Department = "HR",
				Email = "petrov@example.com",
				LdapSourceId = testLdapSource.Id,
				DisplayName = "Petr Petrov",
				Division = "Upravlenie personalom",
				Title = "HR-menedzher",
				Company = "Example Corp",
				DistinguishedName = "cn=Petr Petrov,ou=HR,dc=example,dc=com",
				LastUpdated = DateTime.UtcNow
			}
		};

		context.Contacts.AddRange(testContacts);
		await context.SaveChangesAsync();
	}
}