using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ldap_tels.Models;

// Я использую @Contact.cs для Ldap контактов и контактов созданных вручную.
// Разница в том что в Ldap контакте обязательные поля DistinguishedName, LdapSourceId, LdapSource.
// Мне не хочется создавать отдельную модель LdapContact, наследуемую от Contact с этими полями,
// так как придется усложнять таблицы в базе данных, для их нормализации.
// Мое решение заключается в использовании ViewModel для разделения Ldap контактов и контактов созданных вручную.

public enum ContactType
{
    Manual,
    Ldap
}

public class Contact : IValidatableObject
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
    public string? DistinguishedName { get; set; }
    public int? LdapSourceId { get; set; }
    public LdapSource? LdapSource { get; set; }
    [Required]
    public ContactType ContactType { get; set; }
    [Required]
    public DateTime LastUpdated { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ContactType == ContactType.Ldap)
        {
            if (string.IsNullOrWhiteSpace(DistinguishedName))
                yield return new ValidationResult("DistinguishedName is required for LDAP contacts.", new[] { nameof(DistinguishedName) });

            if (LdapSourceId == null)
                yield return new ValidationResult("LdapSourceId is required for LDAP contacts.", new[] { nameof(LdapSourceId) });

            if (LdapSource == null)
                yield return new ValidationResult("LdapSource is required for LDAP contacts.", new[] { nameof(LdapSource) });
        }
    }
}