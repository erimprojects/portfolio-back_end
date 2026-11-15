namespace portfolio_backend.Auth;

public static class Policies
{
   // regel, moet admin of editor zijn om te kunnen schrijven, anders mag je alleen lezen.
    public const string KanSchrijven = "KanSchrijven";
}