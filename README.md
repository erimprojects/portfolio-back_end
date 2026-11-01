# Portfolio Backend

Backend voor mijn portfoliosite. De API ondersteunt CRUD voor projecten en blogposts.

## Technische gegevens

- **.NET-versie:** .NET 10
- **API-stijl:** ASP.NET Core Web API met controllers
- **Database:** SQL Server (via Docker), met Entity Framework Core
- **Swagger UI:** Swashbuckle.AspNetCore

## Codestructuur

De code is georganiseerd in lagen:

```
portfolio-backend/
├── Controllers/   HTTP-afhandeling (routes en statuscodes)
├── Services/      Applicatielogica en omzetting tussen entity en DTO
├── Data/          EF Core DbContext (PortfolioDb) en migraties
├── Models/        Entities: Project en Blogpost
├── Dtos/          Invoer- en uitvoer-DTO's
└── Program.cs     Registratie van services via dependency injection
```

## Benodigdheden

- .NET 10 SDK
- Een draaiende SQL Server (bijvoorbeeld in Docker)
- De EF Core-tool: `dotnet tool install --global dotnet-ef --version 10.0.0`

## SQL Server configureren

De connectionstring staat niet in de code, maar in **user secrets**.
Voer deze commando's uit in de map `portfolio-backend` (waar `portfolio-backend.csproj` staat).
Vul je eigen gebruikersnaam en wachtwoord van SQL Server in:

```bash
cd portfolio-backend
dotnet user-secrets set "ConnectionStrings:Portfolio" 'Server=localhost,1433;Database=Portfolio;User Id=sa;Password=JOUW_WACHTWOORD;TrustServerCertificate=True'
```

## Database aanmaken

De database wordt aangemaakt met een **EF Core-migratie** (niet met EnsureCreated).
De migratie `InitialCreate` staat al in `Data/Migrations`.

```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet ef database update
```

Dit maakt de database `Portfolio` met de tabellen `Projects` en `Blogposts`.

## Applicatie starten

```bash
cd portfolio-backend
dotnet run
```

Open daarna Swagger UI: http://localhost:5048/swagger

## Endpoints

| Methode | Route | Beschrijving |
|---|---|---|
| GET | /api/projects | Alle projecten |
| GET | /api/projects/{id} | Eén project |
| POST | /api/projects | Project aanmaken |
| PUT | /api/projects/{id} | Project wijzigen |
| DELETE | /api/projects/{id} | Project verwijderen |
| GET | /api/blogposts | Alle blogposts |
| GET | /api/blogposts/{id} | Eén blogpost |
| POST | /api/blogposts | Blogpost aanmaken |
| PUT | /api/blogposts/{id} | Blogpost wijzigen |
| DELETE | /api/blogposts/{id} | Blogpost verwijderen |