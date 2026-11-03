using System.ComponentModel.DataAnnotations;

namespace SmartWarehouse.AdminWeb.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}
