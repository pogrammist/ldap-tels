using PhoneDirectory.Domain.Enums;

namespace PhoneDirectory.Domain.Entities
{
	public class DomainConnection
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Server { get; set; } = string.Empty;
		public int Port { get; set; }
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public bool UseSSL { get; set; }
		public bool IsActive { get; set; }
		public DateTime LastSync { get; set; }
		public ConnectionStatus Status { get; set; }
		public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
		public string BaseDN { get; set; } = string.Empty;
	}
}
