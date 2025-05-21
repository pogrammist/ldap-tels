using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ad_tels.Models;
using ad_tels.Services;

namespace ad_tels.Controllers;

public class AccountController : Controller
{
    private readonly ILdapAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ILdapAuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            // Проверяем учетные данные через LDAP
            if (await _authService.ValidateCredentialsAsync(model.Username, model.Password))
            {
                // Проверяем, входит ли пользователь в группу администраторов
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
                    else
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "У вас нет прав администратора");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль");
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
