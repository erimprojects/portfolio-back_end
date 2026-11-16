using Microsoft.AspNetCore.Identity;
using portfolio_backend.Auth;
using portfolio_backend.Models;

namespace portfolio_backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {// services = di-container van de app en config is de instellingen
        await using var scope = services.CreateAsyncScope(); // usermanager en rolemanager zijn scoped, await using ruimt methode op als klaar is.

    // managers uit de di-container halen, dit is di, maar handmatig omdat dit seeder is en op constructor manier veel opzet vraagt
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();


        foreach(var rol in Roles.AlleRollen)// alle rollen aanmaken als ze niet bestaan.
        {
            if (!await roleManager.RoleExistsAsync(rol)) // checken of rol bestaat
                await roleManager.CreateAsync(new IdentityRole(rol)); // zo niet rol aanmaken 
        }

         // 2. Lokale testgebruikers aanmaken met gegevens uit de user secrets.
        await MaakGebruikerAsync(userManager, config["Seed:EditorEmail"], config["Seed:EditorPassword"], Roles.Editor);
        await MaakGebruikerAsync(userManager, config["Seed:AdminEmail"], config["Seed:AdminPassword"], Roles.Admin);
    }

    // string? = mag null zijn (als de secret niet bestaat).
    private static async Task MaakGebruikerAsync(
        UserManager<User> userManager, string? email, string? wachtwoord, string rol)
    {
        // Geen secrets ingesteld (bijvoorbeeld op de computer van de docent)? Dan overslaan.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(wachtwoord))
            return;

        // Bestaat deze gebruiker al? Dan maken we hem niet nog een keer.
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            // UserName = Email, zodat inloggen met e-mail werkt.
            user = new User { UserName = email, Email = email, EmailConfirmed = true };

            // CreateAsync slaat de gebruiker op en HASHT het wachtwoord.
            var result = await userManager.CreateAsync(user, wachtwoord);

            // Mislukt het (bv. wachtwoord te zwak)? Dan stoppen met een duidelijke foutmelding.
            if (!result.Succeeded)
                throw new Exception("Seeder: gebruiker maken mislukt: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Rol toekennen als hij die nog niet heeft.
        if (!await userManager.IsInRoleAsync(user, rol))
            await userManager.AddToRoleAsync(user, rol);
    }

    }
