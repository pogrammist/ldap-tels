using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PhoneDirectory.Application.DTOs;
using PhoneDirectory.Application.Interfaces;
using PhoneDirectory.Domain.Interfaces;
using PhoneDirectory.Application.Services;
using System.Threading.Tasks;
using PhoneDirectory.Domain.Entities;

namespace PhoneDirectory.Web.Controllers
{
	[Authorize(Roles = "Admin")]
	[ApiController]
	[Route("[controller]")]
	public class AdminController : Controller
	{
		private readonly IActiveDirectoryService _activeDirectoryService;
		private readonly IContactService _contactService;
		private readonly ILogger<AdminController> _logger;

		public AdminController(
			IActiveDirectoryService activeDirectoryService,
			IContactService contactService,
			ILogger<AdminController> logger)
		{
			_activeDirectoryService = activeDirectoryService;
			_contactService = contactService;
			_logger = logger;
		}

		public async Task<IActionResult> Index()
		{
			try
			{
				var domains = await _contactService.GetAllDomainsAsync();
				return View(domains);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting domains list");
				return View(new List<DomainConnectionDto>());
			}
		}

		public IActionResult AddDomain()
		{
			return View(new DomainConnectionDto { Port = 389 });
		}

		[HttpPost]
		public async Task<IActionResult> AddDomain(DomainConnectionDto model)
		{
			if (!ModelState.IsValid)
				return View(model);

			try
			{
				var result = await _contactService.AddDomainAsync(model);
				if (result)
				{
					TempData["Success"] = "Domain added successfully";
					return RedirectToAction(nameof(Index));
				}

				ModelState.AddModelError("", "Failed to add domain");
				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error adding domain");
				ModelState.AddModelError("", "An error occurred while adding the domain");
				return View(model);
			}
		}

		[HttpPost("test-connection")]
		public async Task<IActionResult> TestConnection([FromBody] DomainConnectionDto domain)
		{
			var domainConnection = new DomainConnection
			{
				Server = domain.Server,
				Port = domain.Port,
				Username = domain.Username,
				Password = domain.Password,
				BaseDN = domain.BaseDN
			};

			var result = await _activeDirectoryService.TestConnectionAsync(domainConnection);
			return Ok(result);
		}

		public IActionResult SomeAction()
		{
			TempData["Message"] = "Some message";
			return RedirectToAction("Index");
		}
	}
}
