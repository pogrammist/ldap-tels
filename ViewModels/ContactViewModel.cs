using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using ldap_tels.Models;

public class ContactViewModel
{       
    [Required]
    public int Id { get; set; }
    [Required]
    public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    [Required]
    public ContactType ContactType { get; set; } = ContactType.Manual;
    [Required]
    public DateTime LastUpdated { get; set; }
}