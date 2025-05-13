using ad_tels.Models;
using ad_tels.Services;
using Microsoft.AspNetCore.Mvc;

namespace ad_tels.Controllers;

public class AdminController : Controller
{
    private readonly LdapService _ldapService;
    private readonly ContactService _contactService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        LdapService ldapService, 
        ContactService contactService, 
        ILogger<AdminController> logger)
    {
        _ldapService = ldapService;
        _contactService = contactService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var sources = await _ldapService.GetAllSourcesAsync();
            var totalContacts = await _contactService.GetTotalContactsCountAsync();
            var departments = await _contactService.GetAllDepartmentsAsync();

            ViewBag.TotalContacts = totalContacts;
            ViewBag.TotalDepartments = departments.Count();

            return View(sources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке данных для главной страницы админки");
            return View("Error", new ErrorViewModel { Message = "Не удалось загрузить данные. Пожалуйста, попробуйте позже." });
        }
    }

    public async Task<IActionResult> LdapSources()
    {
        try
        {
            var sources = await _ldapService.GetAllSourcesAsync();
            return View(sources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка LDAP-источников");
            return View("Error", new ErrorViewModel { Message = "Не удалось загрузить список LDAP-источников. Пожалуйста, попробуйте позже." });
        }
    }

    public async Task<IActionResult> LdapSourceDetails(int id)
    {
        try
        {
            var source = await _ldapService.GetSourceByIdAsync(id);
            if (source == null)
            {
                return NotFound();
            }
            return View(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке данных LDAP-источника с ID {Id}", id);
            return View("Error", new ErrorViewModel { Message = $"Не удалось загрузить данные LDAP-источника с ID {id}. Пожалуйста, попробуйте позже." });
        }
    }

    public IActionResult CreateLdapSource()
    {
        return View(new LdapSource());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLdapSource(LdapSource source)
    {
        try
        {
            if (ModelState.IsValid)
            {
                await _ldapService.AddSourceAsync(source);
                return RedirectToAction(nameof(LdapSources));
            }
            return View(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании LDAP-источника");
            ModelState.AddModelError("", "Произошла ошибка при создании LDAP-источника. Пожалуйста, попробуйте позже.");
            return View(source);
        }
    }

    public async Task<IActionResult> EditLdapSource(int id)
    {
        try
        {
            var source = await _ldapService.GetSourceByIdAsync(id);
            if (source == null)
            {
                return NotFound();
            }
            return View(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке данных LDAP-источника с ID {Id} для редактирования", id);
            return View("Error", new ErrorViewModel { Message = $"Не удалось загрузить данные LDAP-источника с ID {id}. Пожалуйста, попробуйте позже." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditLdapSource(int id, LdapSource source)
    {
        try
        {
            if (id != source.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var result = await _ldapService.UpdateSourceAsync(source);
                if (!result)
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(LdapSources));
            }
            return View(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении LDAP-источника с ID {Id}", id);
            ModelState.AddModelError("", "Произошла ошибка при обновлении LDAP-источника. Пожалуйста, попробуйте позже.");
            return View(source);
        }
    }

    public async Task<IActionResult> DeleteLdapSource(int id)
    {
        try
        {
            var source = await _ldapService.GetSourceByIdAsync(id);
            if (source == null)
            {
                return NotFound();
            }
            return View(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке данных LDAP-источника с ID {Id} для удаления", id);
            return View("Error", new ErrorViewModel { Message = $"Не удалось загрузить данные LDAP-источника с ID {id}. Пожалуйста, попробуйте позже." });
        }
    }

    [HttpPost, ActionName("DeleteLdapSource")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLdapSourceConfirmed(int id)
    {
        try
        {
            var result = await _ldapService.DeleteSourceAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(LdapSources));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении LDAP-источника с ID {Id}", id);
            return View("Error", new ErrorViewModel { Message = $"Не удалось удалить LDAP-источник с ID {id}. Пожалуйста, попробуйте позже." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SyncLdapSource(int id)
    {
        try
        {
            var source = await _ldapService.GetSourceByIdAsync(id);
            if (source == null)
            {
                return NotFound();
            }

            await _ldapService.SyncSourceAsync(source);
            TempData["SuccessMessage"] = $"Синхронизация с LDAP-источником {source.Name} успешно выполнена";
            return RedirectToAction(nameof(LdapSourceDetails), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при синхронизации с LDAP-источником с ID {Id}", id);
            TempData["ErrorMessage"] = $"Ошибка при синхронизации: {ex.Message}";
            return RedirectToAction(nameof(LdapSourceDetails), new { id });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SyncAllLdapSources()
    {
        try
        {
            await _ldapService.SyncAllSourcesAsync();
            TempData["SuccessMessage"] = "Синхронизация со всеми LDAP-источниками успешно выполнена";
            return RedirectToAction(nameof(LdapSources));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при синхронизации со всеми LDAP-источниками");
            TempData["ErrorMessage"] = $"Ошибка при синхронизации: {ex.Message}";
            return RedirectToAction(nameof(LdapSources));
        }
    }

    public async Task<IActionResult> Contacts(int page = 1, int pageSize = 20)
    {
        try
        {
            var contacts = await _contactService.GetAllContactsAsync(page, pageSize);
            var totalCount = await _contactService.GetTotalContactsCountAsync();
            
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            return View(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка контактов");
            return View("Error", new ErrorViewModel { Message = "Не удалось загрузить список контактов. Пожалуйста, попробуйте позже." });
        }
    }

    public async Task<IActionResult> ContactDetails(int id)
    {
        try
        {
            var contact = await _contactService.GetContactByIdAsync(id);
            if (contact == null)
            {
                return NotFound();
            }
            return View(contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке данных контакта с ID {Id}", id);
            return View("Error", new ErrorViewModel { Message = $"Не удалось загрузить данные контакта с ID {id}. Пожалуйста, попробуйте позже." });
        }
    }

    public async Task<IActionResult> SearchContacts(string query, int page = 1, int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Contacts));
            }

            var contacts = await _contactService.SearchContactsAsync(query, page, pageSize);
            var totalCount = await _contactService.GetSearchResultsCountAsync(query);
            
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.SearchQuery = query;
            
            return View("Contacts", contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске контактов по запросу: {Query}", query);
            return View("Error", new ErrorViewModel { Message = "Не удалось выполнить поиск контактов. Пожалуйста, попробуйте позже." });
        }
    }

    public async Task<IActionResult> ContactsByDepartment(string department, int page = 1, int pageSize = 20)
    {
        try
        {
            var contacts = await _contactService.GetContactsByDepartmentAsync(department, page, pageSize);
            
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Department = department;
            
            return View("Contacts", contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке контактов отдела: {Department}", department);
            return View("Error", new ErrorViewModel { Message = $"Не удалось загрузить контакты отдела {department}. Пожалуйста, попробуйте позже." });
        }
    }

    public async Task<IActionResult> Departments()
    {
        try
        {
            var departments = await _contactService.GetAllDepartmentsAsync();
            return View(departments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке списка отделов");
            return View("Error", new ErrorViewModel { Message = "Не удалось загрузить список отделов. Пожалуйста, попробуйте позже." });
        }
    }
}
