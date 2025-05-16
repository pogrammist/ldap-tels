using Microsoft.AspNetCore.Mvc;
using ad_tels.Services;
using ad_tels.Models;

namespace ad_tels.Controllers
{
    public class PhoneBookController : Controller
    {
        private readonly ContactService _contactService;

        public PhoneBookController(ContactService contactService)
        {
            _contactService = contactService;
        }

        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DepartmentSortParm"] = sortOrder == "Department" ? "department_desc" : "Department";

            var contacts = await _contactService.GetAllContactsAsync();

            if (!String.IsNullOrEmpty(searchString))
            {
                contacts = contacts.Where(c => 
                    c.DisplayName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    c.Department.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    c.PhoneNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            contacts = sortOrder switch
            {
                "name_desc" => contacts.OrderByDescending(c => c.DisplayName),
                "Department" => contacts.OrderBy(c => c.Department),
                "department_desc" => contacts.OrderByDescending(c => c.Department),
                _ => contacts.OrderBy(c => c.DisplayName),
            };

            return View(contacts);
        }
    }
} 