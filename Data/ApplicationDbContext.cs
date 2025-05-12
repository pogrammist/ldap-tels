using ad_tels.Models;
using Microsoft.EntityFrameworkCore;

namespace ad_tels.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<LdapSource> LdapSources { get; set; } = null!;
    public DbSet<Contact> Contacts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настройка отношений между моделями
        modelBuilder.Entity<Contact>()
            .HasOne(c => c.LdapSource)
            .WithMany()
            .HasForeignKey(c => c.LdapSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Индексы для ускорения поиска
        modelBuilder.Entity<Contact>()
            .HasIndex(c => c.DisplayName);
        
        modelBuilder.Entity<Contact>()
            .HasIndex(c => c.LastName);
        
        modelBuilder.Entity<Contact>()
            .HasIndex(c => c.PhoneNumber);
        
        modelBuilder.Entity<Contact>()
            .HasIndex(c => c.Email);
    }
}
