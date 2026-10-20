using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using portfolio_backend.Data;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Database (EF Core + SQL Server)
builder.Services.AddDbContext<PortfolioDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Portfolio")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Portfolio API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Portfolio API v1");
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();