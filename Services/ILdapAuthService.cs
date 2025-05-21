using System.Threading.Tasks;

namespace ad_tels.Services;

public interface ILdapAuthService
{
	Task<bool> ValidateCredentialsAsync(string username, string password);
	bool IsUserInAdminGroup(string username);
	string GetUserDisplayName(string username);
}