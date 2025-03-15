using PhoneDirectory.Domain.Enums;

namespace PhoneDirectory.Application.DTOs
{
	public class DomainConnectionDto
	{
		public int Id { get; set; }
		public string Server { get; set; } = string.Empty;
		public int Port { get; set; }
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string BaseDN { get; set; } = string.Empty;
	}
}
