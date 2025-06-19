using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ldap_tels.Models;
using Microsoft.AspNetCore.Authorization;

namespace ldap_tels.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/[controller]/[action]")]
public class AccountController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    [Route("/Admin/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // Здесь должен быть ваш [HttpPost] Login (реализуйте по аналогии с текущей логикой)

    [HttpGet]
    [Route("/Admin/Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home", new { area = "" });
    }
} 