using ldap_tels.Models;
using ldap_tels.Services;
using Microsoft.AspNetCore.Mvc;

namespace ldap_tels.Controllers;

public class PhoneBookController : Controller
{
    private readonly ContactService _contactService;
    private readonly ILogger<PhoneBookController> _logger;

    public PhoneBookController(ContactService contactService, ILogger<PhoneBookController> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
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
