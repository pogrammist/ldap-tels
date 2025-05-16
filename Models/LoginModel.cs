using System.ComponentModel.DataAnnotations;

namespace ad_tels.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Введите логин")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}