using ldap_tels.Models;

namespace ldap_tels.Services;

public interface ILdapService
{
    Task<IEnumerable<LdapSource>> GetAllSourcesAsync();
    Task<LdapSource?> GetSourceByIdAsync(int id);
    Task<LdapSource> AddSourceAsync(LdapSource source);
    Task<bool> UpdateSourceAsync(LdapSource source);
    Task<bool> DeleteSourceAsync(int id);
    Task SyncAllSourcesAsync();
    Task SyncSourceAsync(LdapSource source);
}


