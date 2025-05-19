using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ad_tels.Models;
using ad_tels.Services;

namespace ad_tels.Controllers;

public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly ActiveDirectoryService _adService;

    public AccountController(ILogger<AccountController> logger, ActiveDirectoryService adService)
    {
        _logger = logger;
        _adService = adService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            // Проверяем учетные данные через AD
            if (_adService.ValidateCredentials(model.Username, model.Password))
            {
                // Проверяем, входит ли пользователь в группу администраторов
                if (_adService.IsUserInAdminGroup(model.Username))
                {
                    var displayName = _adService.GetUserDisplayName(model.Username);
                    
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
                    ModelState.AddModelError(string.Empty, "У вас нет прав для доступа к админ-панели");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "PhoneBook");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
