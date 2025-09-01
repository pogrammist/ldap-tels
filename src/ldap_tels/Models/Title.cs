namespace ldap_tels.Models;

public class Title
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Weight { get; set; } = 0;

    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
