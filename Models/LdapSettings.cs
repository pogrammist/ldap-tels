namespace ad_tels.Models;

public class LdapSettings
{
	public string Server { get; set; } = string.Empty;
	public int Port { get; set; } = 389;
	public string Domain { get; set; } = string.Empty;
	public string SearchBase { get; set; } = string.Empty;
	public string BindDn { get; set; } = string.Empty;
	public string BindPassword { get; set; } = string.Empty;
	public string UserSearchFilter { get; set; } = "(&(objectClass=user)(sAMAccountName={0}))";
	public string AdminGroup { get; set; } = string.Empty;
}