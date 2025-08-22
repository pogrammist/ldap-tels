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

		// Добавляем справочники
		var division1 = new Division { Name = "Разработка", Weight = 1 };
		var division2 = new Division { Name = "Управление персоналом", Weight = 2 };
		var division3 = new Division { Name = "ManualDiv", Weight = 3 };
		context.Divisions.AddRange(division1, division2, division3);

		var department1 = new Department { Name = "IT", Weight = 1 };
		var department2 = new Department { Name = "HR", Weight = 2 };
		var department3 = new Department { Name = "ManualDept", Weight = 3 };
		context.Departments.AddRange(department1, department2, department3);

		var title1 = new Title { Name = "Старший разработчик", Weight = 1 };
		var title2 = new Title { Name = "HR-менеджер", Weight = 2 };
		var title3 = new Title { Name = "Ручной", Weight = 3 };
		context.Titles.AddRange(title1, title2, title3);

		var company = new Company { Name = "Example Corp", Weight = 1 };
		context.Companies.Add(company);

		await context.SaveChangesAsync();

		// Добавляем тестовые контакты
		var ldapContact1 = new LdapContact
		{
			DisplayName = "Ivan Ivanov",
			PhoneNumber = "+7 (999) 123-45-67",
			DepartmentId = department1.Id,
			DivisionId = division1.Id,
			TitleId = title1.Id,
			CompanyId = company.Id,
			Email = "ivanov@example.com",
			LdapSourceId = testLdapSource.Id,
			DistinguishedName = "cn=Ivan Ivanov,ou=IT,dc=example,dc=com",
			LastUpdated = DateTime.UtcNow
		};
		var ldapContact2 = new LdapContact
		{
			DisplayName = "Petr Petrov",
			PhoneNumber = "+7 (999) 765-43-21",
			DepartmentId = department2.Id,
			DivisionId = division2.Id,
			TitleId = title2.Id,
			CompanyId = company.Id,
			Email = "petrov@example.com",
			LdapSourceId = testLdapSource.Id,
			DistinguishedName = "cn=Petr Petrov,ou=HR,dc=example,dc=com",
			LastUpdated = DateTime.UtcNow
		};
		var manualContact = new ManualContact
		{
			DisplayName = "Manual Contact",
			PhoneNumber = "+7 (999) 111-22-33",
			DepartmentId = department3.Id,
			DivisionId = division3.Id,
			TitleId = title3.Id,
			CompanyId = company.Id,
			Email = "manual@example.com",
			LastUpdated = DateTime.UtcNow
		};

		context.LdapContacts.AddRange(ldapContact1, ldapContact2);
		context.ManualContacts.Add(manualContact);
		await context.SaveChangesAsync();
	}
}