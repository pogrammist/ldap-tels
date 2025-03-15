using PhoneDirectory.Domain.Entities;

namespace PhoneDirectory.Domain.Interfaces
{
	public interface IActiveDirectoryService
	{
		Task<bool> TestConnectionAsync(DomainConnection domain);
		Task<IEnumerable<Contact>> GetContactsAsync(DomainConnection domain);
		Task<bool> SyncContactsAsync(DomainConnection domain);
	}
}
