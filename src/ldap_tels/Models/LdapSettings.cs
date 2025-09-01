namespace LdapAuthDemo.Models;

public class LdapSettings
{
	public string Server { get; set; } = string.Empty;
	public int Port { get; set; }
	public bool UseSsl { get; set; }
	public string SearchBase { get; set; } = string.Empty;
	public string Domain { get; set; } = string.Empty;
}