using Microsoft.AspNetCore.Mvc;

namespace ad_tels.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
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
}
