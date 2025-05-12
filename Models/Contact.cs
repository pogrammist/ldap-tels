namespace ad_tels.Models;

public class Contact
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public int LdapSourceId { get; set; }
    public LdapSource? LdapSource { get; set; }
    public DateTime LastUpdated { get; set; }
}
