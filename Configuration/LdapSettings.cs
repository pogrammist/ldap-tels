namespace ad_tels.Configuration;

public class LdapSettings
{
	public string Server { get; set; } = string.Empty;
	public int Port { get; set; }
	public bool UseSsl { get; set; }
	public string Domain { get; set; } = string.Empty;
	public string SearchBase { get; set; } = string.Empty;
	public string BindUsername { get; set; } = string.Empty;
	public string BindPassword { get; set; } = string.Empty;
	public string AdminGroupDn { get; set; } = string.Empty;
}