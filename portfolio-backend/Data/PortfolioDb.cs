using Microsoft.EntityFrameworkCore;
using portfolio_backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace portfolio_backend.Data;

public class PortfolioDb : IdentityDbContext<User> // <User> zegt welke gebruikersklasse identity moet gebruiken.
{
    public PortfolioDb(DbContextOptions<PortfolioDb> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Blogpost> Blogposts => Set<Blogpost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)// hier stel je in hoe de tabellen eruit zien.
    { // protected deze methode kan alleen in deze klasse en afgeleide klassen worden aangeroepen.
        base.OnModelCreating(modelBuilder); // dit is nodig voor identity, anders werkt het niet.
    }



}
