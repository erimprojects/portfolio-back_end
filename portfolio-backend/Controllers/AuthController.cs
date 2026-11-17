using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using portfolio_backend.Auth;
using portfolio_backend.Dtos;
using portfolio_backend.Models;

namespace portfolio_backend.Controllers;

[ApiController]
[Route("api/auth")]

public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager; // gebruikers maken en rollen geven.
    private readonly SignInManager<User> _signInManager; // wachtwoord checken en cookie zetten.

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
    {// constructor di
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new User { UserName = request.Email, Email = request.Email};// dto entity mapping

        var result = await _userManager.CreateAsync(user, request.Password); // slaat gebruiker op en hast ww
        if (!result.Succeeded)
        {// mislukking zwak ww of email die al bestaat  met de reden door result.succeeded
            return BadRequest(result.Errors.Select(e => e.Description)); //error(400) met reden
        }
        await _userManager.AddToRoleAsync(user, Roles.Gast);// elke nieuwe gebruiker begint met rol gast

        return Ok(new {email = user.Email, rol = Roles.Gast}); 
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, isPersistent: false, lockoutOnFailure: false);
        // Controleert e-mail + wachtwoord. Bij succes zet hij een authenticatiecookie
        // in de response (Set-Cookie). De client stuurt die cookie daarna bij elk request mee.
        // isPersistent: false     = cookie verdwijnt als de browser sluit.
        // lockoutOnFailure: false = account wordt niet geblokkeerd na foute pogingen.

        if (!result.Succeeded)
        {
            return Unauthorized(); // 401 verkeerde email of wachtwoord.
        }
        return Ok(); // hier krijg je 200 en de cookie


    }

}