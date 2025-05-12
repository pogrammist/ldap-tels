using Microsoft.AspNetCore.Mvc;

namespace ad_tels.Controllers;

[ApiController]
[Route("api")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> GetApiInfo()
    {
        return new
        {
            name = "AD Tels API",
            version = "1.0.0",
            description = "Телефонный справочник на основе данных из LDAP сервера",
            endpoints = new[]
            {
                new { path = "/api/ldapsource", description = "Управление LDAP-источниками" },
                new { path = "/api/contact", description = "Поиск и просмотр контактов" }
            }
        };
    }
    
    [HttpGet("health")]
    public ActionResult<object> GetHealthCheck()
    {
        return new
        {
            status = "ok",
            timestamp = DateTime.UtcNow
        };
    }
}
