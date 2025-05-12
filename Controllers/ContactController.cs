using ad_tels.Models;
using ad_tels.Services;
using Microsoft.AspNetCore.Mvc;

namespace ad_tels.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly ContactService _contactService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(ContactService contactService, ILogger<ContactController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Contact>>> GetContacts([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var contacts = await _contactService.GetAllContactsAsync(page, pageSize);
            var totalCount = await _contactService.GetTotalContactsCountAsync();
            
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            
            return Ok(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка контактов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Contact>> GetContactById(int id)
    {
        try
        {
            var contact = await _contactService.GetContactByIdAsync(id);
            if (contact == null)
            {
                return NotFound($"Контакт с ID {id} не найден");
            }
            return Ok(contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении контакта с ID {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Contact>>> SearchContacts([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var contacts = await _contactService.SearchContactsAsync(query, page, pageSize);
            var totalCount = await _contactService.GetSearchResultsCountAsync(query);
            
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            
            return Ok(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске контактов по запросу: {Query}", query);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<string>>> GetAllDepartments()
    {
        try
        {
            var departments = await _contactService.GetAllDepartmentsAsync();
            return Ok(departments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка отделов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("department/{department}")]
    public async Task<ActionResult<IEnumerable<Contact>>> GetContactsByDepartment(string department, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var contacts = await _contactService.GetContactsByDepartmentAsync(department, page, pageSize);
            return Ok(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении контактов отдела: {Department}", department);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}
