using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ldap_tels.Data;
using ldap_tels.Models;
using ldap_tels.Services;
using ldap_tels.ViewModels;

namespace ldap_tels.Tests;

public class ContactServiceTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public ContactServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetAllContactsAsync_OrdersGroups_BySpecAndPagination()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var loggerMock = new Mock<ILogger<ContactService>>();
        var service = new ContactService(context, loggerMock.Object);

        // Создаем тестовые данные
        var division1 = new Division { Id = 1, Name = "Подразделение 1", Weight = 100 };
        var division2 = new Division { Id = 2, Name = "Подразделение 2", Weight = 50 };
        var department1 = new Department { Id = 1, Name = "Отдел 1", Weight = 80 };
        var department2 = new Department { Id = 2, Name = "Отдел 2", Weight = 60 };
        var title1 = new Title { Id = 1, Name = "Должность 1", Weight = 90 };
        var title2 = new Title { Id = 2, Name = "Должность 2", Weight = 70 };

        context.Divisions.AddRange(division1, division2);
        context.Departments.AddRange(department1, department2);
        context.Titles.AddRange(title1, title2);

        var contacts = new List<ManualContact>
        {
            new ManualContact { Id = 1, DisplayName = "Иван Иванов", DivisionId = 1, DepartmentId = 1, TitleId = 1 },
            new ManualContact { Id = 2, DisplayName = "Петр Петров", DivisionId = 1, DepartmentId = 1, TitleId = 2 },
            new ManualContact { Id = 3, DisplayName = "Сидор Сидоров", DivisionId = 1, DepartmentId = 2, TitleId = 1 },
            new ManualContact { Id = 4, DisplayName = "Алексей Алексеев", DivisionId = 2, DepartmentId = null, TitleId = 1 },
            new ManualContact { Id = 5, DisplayName = "Михаил Михайлов", DivisionId = null, DepartmentId = null, TitleId = 1 }
        };

        context.ManualContacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllContactsAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(5, resultList.Count);

        // Проверяем порядок: сначала группы с подразделениями, затем без подразделений
        var firstContact = resultList[0];
        var lastContact = resultList[4];

        Assert.NotNull(firstContact.Division); // Должен быть с подразделением
        Assert.Null(lastContact.Division); // Должен быть без подразделения
    }

    [Fact]
    public async Task GetAllContactsAsync_RespectsWeights()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var loggerMock = new Mock<ILogger<ContactService>>();
        var service = new ContactService(context, loggerMock.Object);

        var division1 = new Division { Id = 1, Name = "A", Weight = 50 };
        var division2 = new Division { Id = 2, Name = "B", Weight = 100 };
        var department1 = new Department { Id = 1, Name = "X", Weight = 60 };
        var department2 = new Department { Id = 2, Name = "Y", Weight = 80 };

        context.Divisions.AddRange(division1, division2);
        context.Departments.AddRange(department1, department2);

        var contacts = new List<ManualContact>
        {
            new ManualContact { Id = 1, DisplayName = "Контакт 1", DivisionId = 1, DepartmentId = 1, TitleId = 1 },
            new ManualContact { Id = 2, DisplayName = "Контакт 2", DivisionId = 2, DepartmentId = null, TitleId = 1 }
        };

        context.ManualContacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllContactsAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);

        // B должен быть первым (вес 100 > 50)
        Assert.Equal("B", resultList[0].Division);
        Assert.Equal("A", resultList[1].Division);
    }

    [Fact]
    public async Task GetAllContactsAsync_HandlesEmptyData()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var loggerMock = new Mock<ILogger<ContactService>>();
        var service = new ContactService(context, loggerMock.Object);

        // Act
        var result = await service.GetAllContactsAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchContactsAsync_ReturnsFilteredResults()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var loggerMock = new Mock<ILogger<ContactService>>();
        var service = new ContactService(context, loggerMock.Object);

        var division = new Division { Id = 1, Name = "Тестовое подразделение", Weight = 100 };
        context.Divisions.Add(division);

        var contacts = new List<ManualContact>
        {
            new ManualContact { Id = 1, DisplayName = "Иван Иванов", DivisionId = 1, DepartmentId = null, TitleId = 1 },
            new ManualContact { Id = 2, DisplayName = "Петр Петров", DivisionId = 1, DepartmentId = null, TitleId = 1 }
        };

        context.ManualContacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SearchContactsAsync("Иван", 1, 10);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Contains(resultList, c => c.DisplayName.Contains("Иван"));
    }
}
