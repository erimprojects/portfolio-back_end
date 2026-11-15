namespace portfolio_backend.Auth;
// static: statische klasse hoeft geen objecten te maken je kan ze direct gebruiken omdat we ze al vaste waarden geven. 
public static class Roles
{
    // const : constante waarde kan niet veranderd worden, is altijd hetzelfde.
    public const string Gast = "Gast";
    public const string Admin = "Admin";
    public const string Editor = "Editor";

    // lijst(array) met alle rollen zodat het makkelijk mee te werken is in de code.
    public static readonly string[] AlleRollen = { Gast, Admin, Editor };

}