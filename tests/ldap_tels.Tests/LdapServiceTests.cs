using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ldap_tels.Data;
using ldap_tels.Models;
using ldap_tels.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ldap_tels.Tests;

public class LdapServiceTests
{
    private sealed class TestableLdapService : LdapService
    {
        private readonly List<LdapContact> _fake;

        public TestableLdapService(ApplicationDbContext ctx, ILogger<LdapService> logger, List<LdapContact> fake)
            : base(ctx, logger)
        {
            _fake = fake;
        }

        protected override List<LdapContact> GetContactsFromLdap(LdapSource source)
        {
            return _fake;
        }
    }

    private static ApplicationDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SyncAllSourcesAsync_OnlyActiveSourcesProcessed()
    {
        using var ctx = CreateInMemoryContext(nameof(SyncAllSourcesAsync_OnlyActiveSourcesProcessed));
        var logger = Mock.Of<ILogger<LdapService>>();

        var active = new LdapSource { Id = 1, Name = "Active", IsActive = true, BaseDn = "", BindDn = "", BindPassword = "", Port = 389, SearchFilter = "(objectClass=*)", Server = "srv", UseSSL = false, LastSyncTime = DateTime.UtcNow };
        var inactive = new LdapSource { Id = 2, Name = "Inactive", IsActive = false, BaseDn = "", BindDn = "", BindPassword = "", Port = 389, SearchFilter = "(objectClass=*)", Server = "srv", UseSSL = false, LastSyncTime = DateTime.UtcNow };
        ctx.LdapSources.AddRange(active, inactive);
        await ctx.SaveChangesAsync();

        var fetched = new List<LdapContact>();
        var sut = new TestableLdapService(ctx, logger, fetched);

        await sut.SyncAllSourcesAsync();

        var processed = await ctx.LdapContacts.CountAsync();
        Assert.Equal(0, processed);
    }

    [Fact]
    public async Task SyncSourceAsync_DoesNotDuplicateTitlesWithinSingleSync()
    {
        using var ctx = CreateInMemoryContext(nameof(SyncSourceAsync_DoesNotDuplicateTitlesWithinSingleSync));
        var logger = Mock.Of<ILogger<LdapService>>();

        var source = new LdapSource { Id = 10, Name = "S", IsActive = true, BaseDn = "", BindDn = "", BindPassword = "", Port = 389, SearchFilter = "(objectClass=*)", Server = "srv", UseSSL = false, LastSyncTime = DateTime.UtcNow };
        ctx.LdapSources.Add(source);
        await ctx.SaveChangesAsync();

        var fake = new List<LdapContact>
        {
            new LdapContact { DistinguishedName = "dn1", DisplayName = "A", Title = new Title { Name = "Engineer" } },
            new LdapContact { DistinguishedName = "dn2", DisplayName = "B", Title = new Title { Name = "Engineer" } }
        };

        var sut = new TestableLdapService(ctx, logger, fake);
        await sut.SyncSourceAsync(source);

        var titles = await ctx.Titles.ToListAsync();
        Assert.Single(titles);
        Assert.Equal("Engineer", titles[0].Name);
    }
}


