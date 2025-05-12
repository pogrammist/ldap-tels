namespace ad_tels.Models;

public class LdapSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public string BaseDn { get; set; } = string.Empty;
    public string BindDn { get; set; } = string.Empty;
    public string BindPassword { get; set; } = string.Empty;
    public string SearchFilter { get; set; } = "(&(objectClass=person)(|(sn=*)(cn=*)))";
    public bool UseSSL { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime LastSyncTime { get; set; }
}
