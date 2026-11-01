using Microsoft.EntityFrameworkCore; // nodig voor UseSqlServer
using Microsoft.OpenApi;             // nodig voor OpenApiInfo (Swagger)
using portfolio_backend.Data;        // PortfolioDb
using portfolio_backend.Services;    // de services en hun interfaces

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICES REGISTREREN (vóór Build) =====

// Controllers aanzetten
builder.Services.AddControllers();

// Database: EF Core met SQL Server
// De connectionstring komt uit de user secrets (ConnectionStrings:Portfolio)
// AddDbContext registreert PortfolioDb als Scoped (één per request)
builder.Services.AddDbContext<PortfolioDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Portfolio")));

// Services (applicatielogica) via dependency injection
// "Vraagt iemand om IProjectService? Geef dan een ProjectService."
// Scoped, omdat de services PortfolioDb gebruiken (die is ook Scoped)
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IBlogpostService, BlogpostService>();

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

app.MapControllers(); // koppelt de routes uit de controllers ([Route], [HttpGet], ...)

app.Run(); // start de server