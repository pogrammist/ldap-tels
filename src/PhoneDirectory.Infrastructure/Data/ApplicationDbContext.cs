using Microsoft.EntityFrameworkCore;
using PhoneDirectory.Domain.Entities;

namespace PhoneDirectory.Infrastructure.Data
{
	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<DomainConnection> DomainConnections { get; set; } = null!;
		public DbSet<Contact> Contacts { get; set; } = null!;

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Contact>()
				.HasOne(c => c.Domain)
				.WithMany(d => d.Contacts)
				.HasForeignKey(c => c.DomainId);

			modelBuilder.Entity<DomainConnection>()
				.Property(d => d.Password)
				.HasColumnType("varchar(max)");
		}
	}
}
