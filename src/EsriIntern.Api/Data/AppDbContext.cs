using EsriIntern.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EsriIntern.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<StatePopulationSnapshot> StatePopulations => Set<StatePopulationSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StatePopulationSnapshot>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.StateName);

            e.Property(x => x.StateName)
                .HasMaxLength(128)
                .IsRequired();
        });
    }
}
