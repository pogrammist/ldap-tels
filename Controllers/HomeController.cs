using ldap_tels.Models;
using ldap_tels.Services;
using Microsoft.AspNetCore.Mvc;

namespace ldap_tels.Controllers;

public class HomeController : Controller
{
    private readonly ContactService _contactService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ContactService contactService, ILogger<HomeController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var contacts = await _contactService.GetAllContactsAsync(1, 20);
            var totalCount = await _contactService.GetTotalContactsCountAsync();
            var departments = await _contactService.GetAllDepartmentsAsync();

            ViewBag.CurrentPage = 1;
            ViewBag.PageSize = 20;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 20.0);
            ViewBag.Departments = departments;

            return View(contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке данных для главной страницы");
            return View("Error", new ErrorViewModel { Message = "Не удалось загрузить данные. Пожалуйста, попробуйте позже." });
        }
    }

    public async Task<IActionResult> Search(string query, int page = 1, int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Index));
            }

            var contacts = await _contactService.SearchContactsAsync(query, page, pageSize);
            var totalCount = await _contactService.GetSearchResultsCountAsync(query);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.SearchQuery = query;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Возвращаем только таблицу (partial)
                return PartialView("_ContactsTablePartial", contacts);
            }

            return View("Index", contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске контактов по запросу: {Query}", query);
            return View("Error", new ErrorViewModel { Message = "Не удалось выполнить поиск контактов. Пожалуйста, попробуйте позже." });
        }
    }

    public async Task<IActionResult> Department(string department, int page = 1, int pageSize = 20)
    {
        try
        {
            var contacts = await _contactService.GetContactsByDepartmentAsync(department, page, pageSize);
            var totalCount = await _contactService.GetContactsByDepartmentCountAsync(department);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.Department = department;

            return View("Index", contacts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке контактов отдела: {Department}", department);
            return View("Error", new ErrorViewModel { Message = $"Не удалось загрузить контакты отдела {department}. Пожалуйста, попробуйте позже." });
        }
    }

    [HttpGet("docs")]
    public IActionResult Docs()
    {
        return View();
    }

    [HttpGet("api")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public ActionResult<object> GetApiInfo()
    {
        return new
        {
            name = "Ldap Tels API",
            version = "1.0.0",
            description = "Телефонный справочник на основе данных из LDAP сервера",
            endpoints = new[]
            {
                new { path = "/api/ldapsource", description = "Управление LDAP-источниками" },
                new { path = "/api/contact", description = "Поиск и просмотр контактов" }
            }
        };
    }

    [HttpGet("api/health")]
    [ApiExplorerSettings(IgnoreApi = false)]
    public ActionResult<object> GetHealthCheck()
    {
        return new
        {
            status = "ok",
            timestamp = DateTime.UtcNow
        };
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

    public async Task<IActionResult> ContactsByDepartment(string department, int page = 1, int pageSize = 20)
    {
        try
        {
            var contacts = await _contactService.GetContactsByDepartmentAsync(department, page, pageSize);
            var totalCount = await _contactService.GetContactsByDepartmentCountAsync(department);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
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
