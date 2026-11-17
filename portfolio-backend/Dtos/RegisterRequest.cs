using System.ComponentModel.DataAnnotations; // hier zitten [Required] en [EmailAddress] in

namespace portfolio_backend.Dtos;

public class RegisterRequest
{
    [Required, EmailAddress]// req mag niet leeg zijn, email moet geldige email ingevoerd zijn = validatie. klopt het niet stuurt apicontroller badrequest terug (400).

    public string Email {get; init; } = ""; // init = aanmaken van het object een keer invullen en daarna niet meer wijzigen.
    
    [Required]
    public string Password {get; init; } = "";
}