using Microsoft.EntityFrameworkCore; // nodig voor UseSqlServer
using Microsoft.OpenApi;             // nodig voor OpenApiInfo (Swagger)
using portfolio_backend.Data;        // PortfolioDb
using portfolio_backend.Services;    // de services en hun interfaces
using Microsoft.AspNetCore.Identity; // nodig voor Identity
using portfolio_backend.Models;      // nodig voor ApplicationUser
using portfolio_backend.Auth;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICES REGISTREREN (vóór Build) =====

// Controllers aanzetten
builder.Services.AddControllers();

// Database: EF Core met SQL Server
// De connectionstring komt uit de user secrets (ConnectionStrings:Portfolio)
// AddDbContext registreert PortfolioDb als Scoped (één per request)
builder.Services.AddDbContext<PortfolioDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Portfolio")));


builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<BlogpostService>();
// identity registreren: zet Identity aan voor een API (cookie-login), en registreert UserManager<User> en SignInManager<User> in DI.
builder.Services.AddIdentityApiEndpoint<User>()
    .AddRoles<IdentityRole>() // rollen aanzetten die ik heb gemaakt + RoleManger vanuit de identity package
    .AddEntityFrameworkStores<PortfolioDb>(); // gebruikers en rollen opslaan in de database via ef core

// autorisatie: policies registreren. Kanschrijven policy word de require role editor en of admin.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.KanSchrijven, policy => 
    policy.RequireRole(Roles.Editor, Roles.Admin));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Portfolio API", Version = "v1" });
});

// ===== APP BOUWEN =====
var app = builder.Build();

// ===== MIDDLEWARE (na Build) =====

// Swagger alleen tijdens het ontwikkelen
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // maakt swagger.json (de beschrijving van de API)
    app.UseSwaggerUI(c => // maakt de testpagina op /swagger
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Portfolio API v1");
    });
}

app.UseHttpsRedirection(); // stuurt http door naar https
// authentication en authorization op volgorde houden. 
app.UseAuthentication(); // leest cookie en valideert de gebruiker (User.Identity.IsAuthenticated)
app.UseAuthorization(); // checkt na de authentication wat de gebruiker mag doen (User.IsInRole("Editor"))

app.MapControllers(); // koppelt de routes uit de controllers ([Route], [HttpGet], ...)

app.Run(); // start de server