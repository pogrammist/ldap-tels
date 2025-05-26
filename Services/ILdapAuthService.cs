using System.Threading.Tasks;

namespace ldap_tels.Services;

public interface ILdapAuthService
{
	Task<bool> ValidateCredentialsAsync(string username, string password);
	Task<bool> IsUserInAdminGroup(string username);
	Task<string> GetUserDisplayName(string username);
}