using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ldap_tels.Models;
using ldap_tels.Services;
using Microsoft.AspNetCore.Authorization;

namespace ldap_tels.Controllers;

public class AdminController : Controller
{
    private readonly ILdapAuthService _authService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILdapAuthService authService, ILogger<AdminController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Administrator"))
        {
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }
        return View();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            if (await _authService.ValidateCredentialsAsync(model.Username, model.Password))
            {
                if (await _authService.IsUserInAdminGroup(model.Username))
                {
                    var displayName = await _authService.GetUserDisplayName(model.Username);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, model.Username),
                        new Claim(ClaimTypes.GivenName, displayName),
                        new Claim(ClaimTypes.Role, "Administrator")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        RedirectUri = returnUrl
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("Пользователь {Username} вошел в систему", model.Username);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                
                ModelState.AddModelError(string.Empty, "У вас нет прав администратора");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль");
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutPost()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Index));
    }
}
