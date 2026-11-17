using System.ComponentModel.DataAnnotations;

namespace portfolio_backend.Dtos;

public class LoginRequest
{
    [Required, EmailAddress]// lees comments RegisterRequest.cs is het zelfde logica
    public string Email { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";
}