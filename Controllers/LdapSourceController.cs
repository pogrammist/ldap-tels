using ad_tels.Models;
using ad_tels.Services;
using Microsoft.AspNetCore.Mvc;

namespace ad_tels.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LdapSourceController : ControllerBase
{
    private readonly LdapService _ldapService;
    private readonly ILogger<LdapSourceController> _logger;

    public LdapSourceController(LdapService ldapService, ILogger<LdapSourceController> logger)
    {
        _ldapService = ldapService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LdapSource>>> GetAllSources()
    {
        try
        {
            var sources = await _ldapService.GetAllSourcesAsync();
            return Ok(sources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка LDAP-источников");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LdapSource>> GetSourceById(int id)
    {
        try
        {
            var source = await _ldapService.GetSourceByIdAsync(id);
            if (source == null)
            {
                return NotFound($"LDAP-источник с ID {id} не найден");
            }
            return Ok(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении LDAP-источника с ID {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost]
    public async Task<ActionResult<LdapSource>> AddSource(LdapSource source)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var addedSource = await _ldapService.AddSourceAsync(source);
            return CreatedAtAction(nameof(GetSourceById), new { id = addedSource.Id }, addedSource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении LDAP-источника");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSource(int id, LdapSource source)
    {
        try
        {
            if (id != source.Id)
            {
                return BadRequest("ID в URL не соответствует ID в теле запроса");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _ldapService.UpdateSourceAsync(source);
            if (!result)
            {
                return NotFound($"LDAP-источник с ID {id} не найден");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении LDAP-источника с ID {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSource(int id)
    {
        try
        {
            var result = await _ldapService.DeleteSourceAsync(id);
            if (!result)
            {
                return NotFound($"LDAP-источник с ID {id} не найден");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении LDAP-источника с ID {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost("{id}/sync")]
    public async Task<IActionResult> SyncSource(int id)
    {
        try
        {
            var source = await _ldapService.GetSourceByIdAsync(id);
            if (source == null)
            {
                return NotFound($"LDAP-источник с ID {id} не найден");
            }

            await _ldapService.SyncSourceAsync(source);
            return Ok(new { message = $"Синхронизация с LDAP-источником {source.Name} успешно выполнена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при синхронизации с LDAP-источником с ID {Id}", id);
            return StatusCode(500, $"Ошибка при синхронизации: {ex.Message}");
        }
    }

    [HttpPost("sync-all")]
    public async Task<IActionResult> SyncAllSources()
    {
        try
        {
            await _ldapService.SyncAllSourcesAsync();
            return Ok(new { message = "Синхронизация со всеми LDAP-источниками успешно выполнена" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при синхронизации со всеми LDAP-источниками");
            return StatusCode(500, $"Ошибка при синхронизации: {ex.Message}");
        }
    }
}
