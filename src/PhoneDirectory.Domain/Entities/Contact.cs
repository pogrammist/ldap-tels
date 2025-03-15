namespace PhoneDirectory.Domain.Entities
{
	public class Contact
	{
		public int Id { get; set; }
		public string DisplayName { get; set; } = string.Empty;
		public string Department { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public string Mobile { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Location { get; set; } = string.Empty;
		public int DomainId { get; set; }
		public virtual DomainConnection Domain { get; set; } = null!;
		public string PhoneNumber { get; set; } = string.Empty;
	}
}
