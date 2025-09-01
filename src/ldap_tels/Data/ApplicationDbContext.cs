using ldap_tels.Models;
using Microsoft.EntityFrameworkCore;

namespace ldap_tels.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<LdapSource> LdapSources { get; set; } = null!;
    public DbSet<Contact> Contacts { get; set; } = null!;
    public DbSet<ManualContact> ManualContacts { get; set; } = null!;
    public DbSet<LdapContact> LdapContacts { get; set; } = null!;
    public DbSet<Division> Divisions { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Title> Titles { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TPT mapping
        modelBuilder.Entity<Contact>().ToTable("Contacts");
        modelBuilder.Entity<ManualContact>().ToTable("ManualContacts");
        modelBuilder.Entity<LdapContact>().ToTable("LdapContacts");

        // Навигационные свойства
        modelBuilder.Entity<Contact>()
            .HasOne(c => c.Division)
            .WithMany(d => d.Contacts)
            .HasForeignKey(c => c.DivisionId)
            .IsRequired(false);
        modelBuilder.Entity<Contact>()
            .HasOne(c => c.Department)
            .WithMany(d => d.Contacts)
            .HasForeignKey(c => c.DepartmentId)
            .IsRequired(false);
        modelBuilder.Entity<Contact>()
            .HasOne(c => c.Title)
            .WithMany(t => t.Contacts)
            .HasForeignKey(c => c.TitleId)
            .IsRequired(false);
        modelBuilder.Entity<Contact>()
            .HasOne(c => c.Company)
            .WithMany(t => t.Contacts)
            .HasForeignKey(c => c.CompanyId)
            .IsRequired(false);

        // Индексы для ускорения поиска
        modelBuilder.Entity<Contact>().HasIndex(c => c.DisplayName);
        modelBuilder.Entity<Contact>().HasIndex(c => c.PhoneNumber);
        modelBuilder.Entity<Contact>().HasIndex(c => c.Email);
        modelBuilder.Entity<Contact>().HasIndex(c => c.DivisionId);
        modelBuilder.Entity<Contact>().HasIndex(c => c.DepartmentId);
        modelBuilder.Entity<Contact>().HasIndex(c => c.TitleId);
        modelBuilder.Entity<Contact>().HasIndex(c => c.CompanyId);

        // LdapContact: связь с LdapSource
        modelBuilder.Entity<LdapContact>()
            .HasOne(l => l.LdapSource)
            .WithMany()
            .HasForeignKey(l => l.LdapSourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
