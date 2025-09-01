using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ldap_tels.Models;

public abstract class Contact
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    [EmailAddress]
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int? DivisionId { get; set; }
    public Division? Division { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? TitleId { get; set; }
    public Title? Title { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ManualContact : Contact
{
    // специфичных полей нет
}

public class LdapContact : Contact
{
    [Required]
    public string DistinguishedName { get; set; } = string.Empty;
    [Required]
    public int LdapSourceId { get; set; }
    public LdapSource LdapSource { get; set; } = null!;
}