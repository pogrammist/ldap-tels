using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ad_tels.Models;
using ad_tels.Services;
using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Configuration;

namespace ad_tels.Controllers;

public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly ActiveDirectoryService _adService;
    private readonly IConfiguration _configuration;

    public AccountController(ILogger<AccountController> logger, ActiveDirectoryService adService, IConfiguration configuration)
    {
        _logger = logger;
        _adService = adService;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            // Проверяем учетные данные через AD
            if (_adService.ValidateCredentials(model.Username, model.Password))
            {
                // Проверяем, входит ли пользователь в группу пользователь домена
                if (_adService.IsUserInGroup(model.Username, _configuration["ActiveDirectory:Domain"]))
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
                        IsPersistent = model.RememberMe
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
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
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

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
} 