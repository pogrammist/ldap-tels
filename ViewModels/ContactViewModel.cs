using ldap_tels.Models;

namespace ldap_tels.ViewModels;

public enum ContactType
{
    Manual,
    Ldap
}

public class ContactViewModel
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Division { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public ContactType ContactType { get; set; }
    public string? DistinguishedName { get; set; }
    public int? LdapSourceId { get; set; }
    public LdapSource? LdapSource { get; set; }
    public DateTime LastUpdated { get; set; }
}