namespace PhoneDirectory.Application.DTOs
{
    public class ContactDto
    {
    	public int Id { get; set; }
    	public string DisplayName { get; set; } = string.Empty;
    	public string Department { get; set; } = string.Empty;
    	public string Title { get; set; } = string.Empty;
    	public string Phone { get; set; } = string.Empty;
    	public string Mobile { get; set; } = string.Empty;
    	public string Email { get; set; } = string.Empty;
    	public string Location { get; set; } = string.Empty;
    	public string DomainName { get; set; } = string.Empty;
    }
}
