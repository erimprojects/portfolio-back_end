# Portfolio Backend

Backend voor mijn portfoliosite. De API ondersteunt CRUD voor **projecten** en **blogposts**, beveiligd met **ASP.NET Core Identity** (registratie, cookie-login en rollen) en getest met **xUnit**.

**Student:** Erim Kocak · klas 3

## Technische gegevens

- **.NET-versie:** .NET 10
- **API-stijl:** ASP.NET Core Web API met controllers
- **Database:** SQL Server 2022 (in Docker), met Entity Framework Core
- **Authenticatie:** ASP.NET Core Identity met authenticatiecookie
- **Autorisatie:** rollen `Gast`, `Editor`, `Admin` en policy `KanSchrijven`
- **Swagger UI:** Swashbuckle.AspNetCore
- **Tests:** xUnit + WebApplicationFactory (integratietests via HTTP), SQLite in-memory als testdatabase

## Codestructuur

```
portfolio-back_end/
├── portfolio-backend.slnx       Solution (API + testproject)
├── portfolio-backend/           De API
│   ├── Auth/                    Rolnamen (Roles) en policynamen (Policies)
│   ├── Controllers/             HTTP-afhandeling: Projects, Blogposts, Auth
│   ├── Services/                Logica en mapping tussen entity en DTO
│   ├── Data/                    PortfolioDb (IdentityDbContext), DbSeeder, migraties
│   ├── Models/                  Entities: Project, Blogpost, User
│   ├── Dtos/                    Request- en response-DTO's (met validatie)
│   └── Program.cs               DI-registratie, Identity, policies en middleware
└── Portfolio.Tests/             xUnit-testproject
    ├── PortfolioApiFactory.cs   Start de API in het geheugen met een aparte testdatabase
    ├── TestHelpers.cs           Inloggen als Gast/Editor/Admin en testdata
    ├── ProjectsTests.cs         Tests voor de 5 projectroutes
    ├── BlogpostsTests.cs        Tests voor de 5 blogpostroutes
    └── AuthTests.cs             Tests voor registratie en login
```

---

## 1. Benodigdheden

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- De EF Core-tool:

```bash
dotnet tool install --global dotnet-ef --version 10.0.0
```

Repository ophalen:

```bash
git clone <repo-url>
cd portfolio-back_end
```

## 2. Gebruikte NuGet-packages

Deze packages staan al in de `.csproj`-bestanden. `dotnet restore` (of `dotnet build`) haalt ze automatisch op. Ter info, zo zijn ze toegevoegd:

**API** (in de map `portfolio-backend`):

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.0
dotnet add package Swashbuckle.AspNetCore
```

**Testproject** (in de map `Portfolio.Tests`):

```bash
dotnet new xunit -n Portfolio.Tests -f net10.0          # vanuit de repo-map
dotnet sln portfolio-backend.slnx add Portfolio.Tests/Portfolio.Tests.csproj
dotnet add reference ../portfolio-backend/portfolio-backend.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.0
```

Alles herstellen:

```bash
dotnet restore
```

## 3. Database starten (SQL Server in Docker)

Kies zelf een wachtwoord (minimaal 8 tekens met hoofdletter, kleine letter, cijfer en teken). Gebruik **enkele aanhalingstekens**, zodat een `!` geen problemen geeft in zsh.

```bash
docker run --platform linux/amd64 \
  --name sqlserver \
  -e 'ACCEPT_EULA=Y' \
  -e 'MSSQL_SA_PASSWORD=<JouwSqlWachtwoord>' \
  -p 1433:1433 \
  -v sqlserver_data:/var/opt/mssql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

- `--platform linux/amd64` is nodig op een Mac met Apple-chip (zet in Docker Desktop eventueel *Use Rosetta* aan).
- `-v sqlserver_data:...` bewaart de data in een volume, zodat die blijft bestaan.

Controleren of de container draait:

```bash
docker ps
```

Later opnieuw starten (na een herstart van de computer):

```bash
docker start sqlserver
```

## 4. Geheimen instellen (user secrets)

Wachtwoorden en de connectionstring staan **niet in de repository**, maar in .NET user secrets (alleen op je eigen computer). Voer uit in de map `portfolio-backend`:

```bash
cd portfolio-backend

# Verbinding met SQL Server (zelfde wachtwoord als bij docker run)
dotnet user-secrets set "ConnectionStrings:Portfolio" 'Server=localhost,1433;Database=Portfolio;User Id=sa;Password=<JouwSqlWachtwoord>;TrustServerCertificate=True'

# Lokale testgebruikers voor de rollen Editor en Admin (zie stap 7)
dotnet user-secrets set "Seed:EditorEmail" "editor@portfolio.local"
dotnet user-secrets set "Seed:EditorPassword" '<EditorWachtwoord>'
dotnet user-secrets set "Seed:AdminEmail" "admin@portfolio.local"
dotnet user-secrets set "Seed:AdminPassword" '<AdminWachtwoord>'
```

Identity-wachtwoorden moeten minimaal 6 tekens hebben, met een hoofdletter, een kleine letter, een cijfer en een speciaal teken.

Controleren:

```bash
dotnet user-secrets list
```

## 5. Migraties toepassen (inclusief Identity)

De database en alle tabellen worden gemaakt met **EF Core-migraties**. Ze staan al in `Data/Migrations` (o.a. `InitialCreate` en `AddIdentity`).

```bash
cd portfolio-backend
export ASPNETCORE_ENVIRONMENT=Development   # zodat de user secrets gelezen worden
dotnet ef database update
```

Dit maakt de database `Portfolio` met:

- `Projects` en `Blogposts`
- de Identity-tabellen `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` (en overige `AspNet*`-tabellen). Wachtwoorden staan alleen gehasht in `AspNetUsers.PasswordHash`.

Zelf een nieuwe migratie maken na een wijziging in de models:

```bash
dotnet ef migrations add <NaamVanMigratie>
dotnet ef database update
```

## 6. Applicatie starten

```bash
cd portfolio-backend
dotnet run
```

Swagger UI: http://localhost:5048/swagger

## 7. Gebruikers en rollen

| Rol | Rechten | Hoe krijg je hem? |
|---|---|---|
| **Gast** | alleen lezen (GET) | automatisch bij registratie via `POST /api/auth/register` |
| **Editor** | lezen + aanmaken, wijzigen, verwijderen | alleen lokaal (seeder of handmatig) |
| **Admin** | lezen + aanmaken, wijzigen, verwijderen | alleen lokaal (seeder of handmatig) |

Publieke registratie geeft **nooit** Editor of Admin. De policy `KanSchrijven` vereist expliciet `Editor` **of** `Admin`.

### Gewone gebruiker (Gast) maken

Via Swagger: `POST /api/auth/register` met

```json
{ "email": "gast@portfolio.local", "password": "<GastWachtwoord>" }
```

Inloggen: `POST /api/auth/login` met dezelfde gegevens. De API stuurt een authenticatiecookie terug (`Set-Cookie: .AspNetCore.Identity.Application=...`), die de browser bij volgende requests meestuurt.

### Editor en Admin maken (lokaal script)

Bij `dotnet run` in de omgeving **Development** draait `Data/DbSeeder.cs` automatisch. Die:

1. maakt de rollen `Gast`, `Editor` en `Admin` aan als ze nog niet bestaan;
2. maakt de gebruikers uit de user secrets (`Seed:EditorEmail`, `Seed:AdminEmail`) aan en geeft ze de rol Editor of Admin.

De seeder controleert eerst of iets al bestaat, dus vaker starten geeft geen dubbele gegevens.

### Handmatig een rol toekennen (in de lokale database)

Een bestaande gebruiker Editor maken, bijvoorbeeld in DBeaver:

```sql
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u, AspNetRoles r
WHERE u.NormalizedEmail = 'GAST@PORTFOLIO.LOCAL'
  AND r.NormalizedName = 'EDITOR';
```

**Let op:** de rollen zitten in de cookie. Na een rolwijziging moet de gebruiker **opnieuw inloggen**.

## 8. Endpoints en toegang

| Methode | Route | Toegang | Resultaten |
|---|---|---|---|
| POST | /api/auth/register | iedereen | 200 · 400 (ongeldig e-mailadres, zwak wachtwoord, bestaat al) |
| POST | /api/auth/login | iedereen | 200 + cookie · 400 (lege invoer) · 401 (fout wachtwoord) |
| GET | /api/projects | iedereen | 200 |
| GET | /api/projects/{id} | iedereen | 200 · 404 |
| POST | /api/projects | Editor, Admin | 201 · 400 · 401 · 403 |
| PUT | /api/projects/{id} | Editor, Admin | 204 · 400 · 401 · 403 · 404 |
| DELETE | /api/projects/{id} | Editor, Admin | 204 · 401 · 403 · 404 |
| GET | /api/blogposts | iedereen | 200 |
| GET | /api/blogposts/{id} | iedereen | 200 · 404 |
| POST | /api/blogposts | Editor, Admin | 201 · 400 · 401 · 403 |
| PUT | /api/blogposts/{id} | Editor, Admin | 204 · 400 · 401 · 403 · 404 |
| DELETE | /api/blogposts/{id} | Editor, Admin | 204 · 401 · 403 · 404 |

- **401** = niet ingelogd · **403** = ingelogd, maar geen Editor of Admin.
- De server dwingt dit af met `[Authorize(Policy = Policies.KanSchrijven)]`.

## 9. Tests uitvoeren

Vanuit de repo-map (`portfolio-back_end`):

```bash
dotnet test
```

- De tests starten de echte API in het geheugen (`WebApplicationFactory`) en sturen echte HTTP-requests.
- Ze gebruiken een **eigen SQLite-database in het geheugen**, dus de gewone SQL Server-database wordt niet aangeraakt en Docker hoeft niet te draaien.
- De testgebruikers (Editor en Admin) worden met dezelfde seeder aangemaakt, maar met vaste testgegevens die alleen in de testdatabase bestaan.

Uitgebreidere uitvoer (elke testnaam):

```bash
dotnet test --logger "console;verbosity=normal"
```

Code coverage meten (coverlet zit al in het testproject):

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Het rapport komt in `Portfolio.Tests/TestResults/<id>/coverage.cobertura.xml`.

## 10. Veelvoorkomende fouten

| Fout | Oorzaak | Oplossing |
|---|---|---|
| `Connection refused` | SQL Server-container staat uit | `docker start sqlserver` |
| `Login failed for user 'sa'` | wachtwoord connectionstring ≠ container | user secret opnieuw zetten (stap 4) |
| Seeder: `Passwords must have...` | seed-wachtwoord te zwak | sterker wachtwoord in user secrets |
| 403 na rol toekennen | oude cookie | opnieuw inloggen |
