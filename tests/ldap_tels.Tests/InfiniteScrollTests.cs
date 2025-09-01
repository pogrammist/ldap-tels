using ldap_tels.Data;
using ldap_tels.Models;
using ldap_tels.Services;
using ldap_tels.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ldap_tels.Tests;

public class InfiniteScrollTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Mock<ILogger<ContactService>> _loggerMock;

    public InfiniteScrollTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _loggerMock = new Mock<ILogger<ContactService>>();
    }

    [Fact]
    public async Task GetAllContactsAsync_FirstPage_ReturnsCorrectPageSize()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        // Создаем 15 контактов
        var contacts = Enumerable.Range(1, 15)
            .Select(i => new Contact 
            { 
                Id = i, 
                DisplayName = $"Contact {i}", 
                Division = $"Division {(i % 3) + 1}" 
            })
            .ToList();

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllContactsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal("Contact 1", result.First().DisplayName);
        Assert.Equal("Contact 10", result.Last().DisplayName);
    }

    [Fact]
    public async Task GetAllContactsAsync_SecondPage_ReturnsCorrectData()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        var contacts = Enumerable.Range(1, 15)
            .Select(i => new Contact 
            { 
                Id = i, 
                DisplayName = $"Contact {i}", 
                Division = $"Division {(i % 3) + 1}" 
            })
            .ToList();

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllContactsAsync(page: 2, pageSize: 10);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal("Contact 11", result.First().DisplayName);
        Assert.Equal("Contact 15", result.Last().DisplayName);
    }

    [Fact]
    public async Task GetAllContactsAsync_LastPage_ReturnsRemainingData()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        var contacts = Enumerable.Range(1, 25)
            .Select(i => new Contact 
            { 
                Id = i, 
                DisplayName = $"Contact {i}", 
                Division = $"Division {(i % 3) + 1}" 
            })
            .ToList();

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllContactsAsync(page: 3, pageSize: 10);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal("Contact 21", result.First().DisplayName);
        Assert.Equal("Contact 25", result.Last().DisplayName);
    }

    [Fact]
    public async Task GetAllContactsAsync_PageBeyondData_ReturnsEmpty()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        var contacts = Enumerable.Range(1, 10)
            .Select(i => new Contact 
            { 
                Id = i, 
                DisplayName = $"Contact {i}", 
                Division = $"Division {(i % 3) + 1}" 
            })
            .ToList();

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllContactsAsync(page: 3, pageSize: 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchContactsAsync_Pagination_WorksCorrectly()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        var contacts = new List<Contact>
        {
            new Contact { Id = 1, DisplayName = "John Smith", Division = "IT" },
            new Contact { Id = 2, DisplayName = "Jane Smith", Division = "HR" },
            new Contact { Id = 3, DisplayName = "Bob Smith", Division = "IT" },
            new Contact { Id = 4, DisplayName = "Alice Johnson", Division = "HR" }
        };

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SearchContactsAsync("Smith", page: 1, pageSize: 2);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, contact => Assert.Contains("Smith", contact.DisplayName));
    }

    [Fact]
    public async Task GetContactsByDivisionAsync_Pagination_RespectsPageSize()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        var contacts = Enumerable.Range(1, 15)
            .Select(i => new Contact 
            { 
                Id = i, 
                DisplayName = $"Contact {i}", 
                Division = "IT" 
            })
            .ToList();

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetContactsByDivisionAsync("IT", page: 1, pageSize: 5);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.All(result, contact => Assert.Equal("IT", contact.Division));
    }

    [Fact]
    public async Task GetContactsByDepartmentAsync_Pagination_WorksCorrectly()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        var contacts = Enumerable.Range(1, 12)
            .Select(i => new Contact 
            { 
                Id = i, 
                DisplayName = $"Contact {i}", 
                Department = "Development" 
            })
            .ToList();

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetContactsByDepartmentAsync("Development", page: 2, pageSize: 5);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.All(result, contact => Assert.Equal("Development", contact.Department));
    }

    [Fact]
    public async Task GetContactsByTitleAsync_Pagination_MaintainsGroupOrder()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var service = new ContactService(context, _loggerMock.Object);
        
        var contacts = new List<Contact>
        {
            new Contact { Id = 1, DisplayName = "Manager 1", Title = "Manager", Division = "IT", Department = "Dev" },
            new Contact { Id = 2, DisplayName = "Manager 2", Title = "Manager", Division = null, Department = "HR" },
            new Contact { Id = 3, DisplayName = "Manager 3", Title = "Manager", Division = null, Department = null },
            new Contact { Id = 4, DisplayName = "Manager 4", Title = "Manager", Division = "IT", Department = null }
        };

        context.Contacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetContactsByTitleAsync("Manager", page: 1, pageSize: 10);

        // Assert
        Assert.Equal(4, result.Count);
        
        // Проверяем, что порядок групп сохраняется
        var resultList = result.ToList();
        Assert.NotNull(resultList[0].Division); // Первые два должны быть с подразделениями
        Assert.NotNull(resultList[1].Division);
        Assert.Null(resultList[2].Division);   // Третий без подразделения, но с отделом
        Assert.NotNull(resultList[2].Department);
        Assert.Null(resultList[3].Division);   // Четвертый без подразделения и отдела
        Assert.Null(resultList[3].Department);
    }
}
