using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ldap_tels.Data;
using ldap_tels.Models;
using ldap_tels.Services;
using ldap_tels.ViewModels;

namespace ldap_tels.Tests;

public class InfiniteScrollTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public InfiniteScrollTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetAllContactsAsync_RespectsPageSize()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var loggerMock = new Mock<ILogger<ContactService>>();
        var service = new ContactService(context, loggerMock.Object);

        var division = new Division { Id = 1, Name = "Тестовое подразделение", Weight = 100 };
        context.Divisions.Add(division);

        var contacts = new List<ManualContact>();
        for (int i = 1; i <= 25; i++)
        {
            contacts.Add(new ManualContact 
            { 
                Id = i, 
                DisplayName = $"Контакт {i}", 
                DivisionId = 1, 
                DepartmentId = null, 
                TitleId = 1 
            });
        }

        context.ManualContacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllContactsAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(10, resultList.Count);
    }

    [Fact]
    public async Task GetAllContactsAsync_RespectsPagination()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var loggerMock = new Mock<ILogger<ContactService>>();
        var service = new ContactService(context, loggerMock.Object);

        var division = new Division { Id = 1, Name = "Тестовое подразделение", Weight = 100 };
        context.Divisions.Add(division);

        var contacts = new List<ManualContact>();
        for (int i = 1; i <= 15; i++)
        {
            contacts.Add(new ManualContact 
            { 
                Id = i, 
                DisplayName = $"Контакт {i}", 
                DivisionId = 1, 
                DepartmentId = null, 
                TitleId = 1 
            });
        }

        context.ManualContacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var page1 = await service.GetAllContactsAsync(1, 10);
        var page2 = await service.GetAllContactsAsync(2, 10);

        // Assert
        Assert.NotNull(page1);
        Assert.NotNull(page2);
        var page1List = page1.ToList();
        var page2List = page2.ToList();
        Assert.Equal(10, page1List.Count);
        Assert.Equal(5, page2List.Count);

        // Проверяем, что страницы не пересекаются
        var page1Ids = page1List.Select(c => c.Id).ToHashSet();
        var page2Ids = page2List.Select(c => c.Id).ToHashSet();
        Assert.Empty(page1Ids.Intersect(page2Ids));
    }

    [Fact]
    public async Task GetContactsByDivisionAsync_RespectsPagination()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var loggerMock = new Mock<ILogger<ContactService>>();
        var service = new ContactService(context, loggerMock.Object);

        var division = new Division { Id = 1, Name = "Тестовое подразделение", Weight = 100 };
        context.Divisions.Add(division);

        var contacts = new List<ManualContact>();
        for (int i = 1; i <= 20; i++)
        {
            contacts.Add(new ManualContact 
            { 
                Id = i, 
                DisplayName = $"Контакт {i}", 
                DivisionId = 1, 
                DepartmentId = null, 
                TitleId = 1 
            });
        }

        context.ManualContacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetContactsByDivisionAsync("Тестовое подразделение", 1, 15);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(15, resultList.Count);
    }

    [Fact]
    public async Task GetContactsByDepartmentAsync_RespectsPagination()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var loggerMock = new Mock<ILogger<ContactService>>();
        var service = new ContactService(context, loggerMock.Object);

        var division = new Division { Id = 1, Name = "Тестовое подразделение", Weight = 100 };
        var department = new Department { Id = 1, Name = "Тестовый отдел", Weight = 80 };
        context.Divisions.Add(division);
        context.Departments.Add(department);

        var contacts = new List<ManualContact>();
        for (int i = 1; i <= 18; i++)
        {
            contacts.Add(new ManualContact 
            { 
                Id = i, 
                DisplayName = $"Контакт {i}", 
                DivisionId = 1, 
                DepartmentId = 1, 
                TitleId = 1 
            });
        }

        context.ManualContacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetContactsByDepartmentAsync("Тестовый отдел", 1, 12);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(12, resultList.Count);
    }

    [Fact]
    public async Task GetContactsByTitleAsync_RespectsPagination()
    {
        // Arrange
        using var context = new ApplicationDbContext(_options);
        var loggerMock = new Mock<ILogger<ContactService>>();
        var service = new ContactService(context, loggerMock.Object);

        var title = new Title { Id = 1, Name = "Тестовая должность", Weight = 90 };
        context.Titles.Add(title);

        var contacts = new List<ManualContact>();
        for (int i = 1; i <= 22; i++)
        {
            contacts.Add(new ManualContact 
            { 
                Id = i, 
                DisplayName = $"Контакт {i}", 
                DivisionId = null, 
                DepartmentId = null, 
                TitleId = 1 
            });
        }

        context.ManualContacts.AddRange(contacts);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetContactsByTitleAsync("Тестовая должность", 1, 8);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(8, resultList.Count);
    }
}
