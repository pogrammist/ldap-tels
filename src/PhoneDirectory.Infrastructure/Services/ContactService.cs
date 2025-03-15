using PhoneDirectory.Application.DTOs;
using PhoneDirectory.Application.Interfaces;
using PhoneDirectory.Domain.Entities;
using PhoneDirectory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PhoneDirectory.Infrastructure.Services;

public class ContactService : IContactService
{
	private readonly ApplicationDbContext _context;

	public ContactService(ApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<bool> AddDomainAsync(DomainConnectionDto domainDto)
	{
		var domain = new DomainConnection
		{
			Server = domainDto.Server,
			Port = domainDto.Port,
			Username = domainDto.Username,
			Password = domainDto.Password,
			BaseDN = domainDto.BaseDN
		};

		_context.DomainConnections.Add(domain);
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<DomainConnectionDto?> GetDomainByIdAsync(int id)
	{
		var domain = await _context.DomainConnections.FindAsync(id);
		if (domain == null) return null;

		return new DomainConnectionDto
		{
			Id = domain.Id,
			Server = domain.Server,
			Port = domain.Port,
			Username = domain.Username,
			Password = domain.Password,
			BaseDN = domain.BaseDN
		};
	}

	public async Task<IEnumerable<DomainConnectionDto>> GetAllDomainsAsync()
	{
		return await _context.DomainConnections
			.Select(d => new DomainConnectionDto
			{
				Id = d.Id,
				Server = d.Server,
				Port = d.Port,
				Username = d.Username,
				Password = d.Password,
				BaseDN = d.BaseDN
			})
			.ToListAsync();
	}
}