using PhoneDirectory.Application.DTOs;

namespace PhoneDirectory.Application.Interfaces;

public interface IContactService
{
	Task<bool> AddDomainAsync(DomainConnectionDto domain);
	Task<DomainConnectionDto?> GetDomainByIdAsync(int id);
	Task<IEnumerable<DomainConnectionDto>> GetAllDomainsAsync();
}
