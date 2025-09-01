using ldap_tels.Data;
using ldap_tels.Models;
using ldap_tels.Services;
using ldap_tels.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ldap_tels.Tests;

public class ContactServiceTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Mock<ILogger<ContactService>> _loggerMock;

    public ContactServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _loggerMock = new Mock<ILogger<ContactService>>();
    }

    [Fact]
    public async Task GetAllContactsAsync_OrdersGroups_BySpecifiedOrder()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        // Создаем тестовые данные с разными группами
        var contacts = new List<Contact>
        {
            // Группа 0: с подразделением и отделом
            new Contact { Id = 1, DisplayName = "Contact 1", Division = "Division 1", Department = "Department 1" },
            // Группа 0: с подразделением, без отдела
            new Contact { Id = 2, DisplayName = "Contact 2", Division = "Division 2", Department = null },
            // Группа 1: без подразделения, с отделом
            new Contact { Id = 3, DisplayName = "Contact 3", Division = null, Department = "Department 3" },
            // Группа 2: без подразделения и отдела
            new Contact { Id = 4, DisplayName = "Contact 4", Division = null, Department = null }
        };

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllContactsAsync();

        // Assert
        Assert.Equal(4, result.Count);
        
        // Проверяем порядок групп: сначала группа 0, потом группа 1, потом группа 2
        var resultList = result.ToList();
        Assert.Contains("Division 1", resultList[0].Division);
        Assert.Contains("Division 2", resultList[1].Division);
        Assert.Null(resultList[2].Division);
        Assert.Contains("Department 3", resultList[2].Department);
        Assert.Null(resultList[3].Division);
        Assert.Null(resultList[3].Department);
    }

    [Fact]
    public async Task GetAllContactsAsync_RespectsWeights_WithinGroups()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        // Создаем тестовые данные с весами
        var divisions = new List<Division>
        {
            new Division { Id = 1, Name = "Division A", Weight = 10 },
            new Division { Id = 2, Name = "Division B", Weight = 20 }
        };
        
        var departments = new List<Department>
        {
            new Department { Id = 1, Name = "Department X", Weight = 5 },
            new Department { Id = 2, Name = "Department Y", Weight = 15 }
        };

        context.Divisions.AddRange(divisions);
        context.Departments.AddRange(departments);
        await context.SaveChangesAsync();

        var contacts = new List<Contact>
        {
            new Contact { Id = 1, DisplayName = "Contact 1", DivisionId = 2, DepartmentId = 2 }, // Высокие веса
            new Contact { Id = 2, DisplayName = "Contact 2", DivisionId = 1, DepartmentId = 1 }, // Низкие веса
            new Contact { Id = 3, DisplayName = "Contact 3", DivisionId = 2, DepartmentId = 1 }, // Высокий вес подразделения
            new Contact { Id = 4, DisplayName = "Contact 4", DivisionId = 1, DepartmentId = 2 }  // Низкий вес подразделения
        };

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllContactsAsync();

        // Assert
        Assert.Equal(4, result.Count);
        
        // Проверяем, что контакты с высокими весами идут первыми
        var resultList = result.ToList();
        Assert.Equal("Division B", resultList[0].Division);
        Assert.Equal("Division B", resultList[1].Division);
        Assert.Equal("Division A", resultList[2].Division);
        Assert.Equal("Division A", resultList[3].Division);
    }

    [Fact]
    public async Task GetAllContactsAsync_HandlesEmptyData_Correctly()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);

        // Act
        var result = await service.GetAllContactsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchContactsAsync_MaintainsGroupOrder()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        var contacts = new List<Contact>
        {
            new Contact { Id = 1, DisplayName = "John Doe", Division = "IT", Department = "Development" },
            new Contact { Id = 2, DisplayName = "Jane Smith", Division = null, Department = "HR" },
            new Contact { Id = 3, DisplayName = "Bob Johnson", Division = null, Department = null }
        };

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SearchContactsAsync("John");

        // Assert
        Assert.Single(result);
        Assert.Equal("John Doe", result.First().DisplayName);
        Assert.Equal("IT", result.First().Division);
    }
}
