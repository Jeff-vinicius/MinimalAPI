using Microsoft.EntityFrameworkCore;
using MinimalAPI.Models;

namespace MinimalAPI.Data
{
    public class MinimalContextDb : DbContext
    {
        public MinimalContextDb(DbContextOptions<MinimalContextDb> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Cliente>()
                .Property(p => p.Nome)
                .IsRequired()
                .HasColumnType("varchar(200)");

            modelBuilder.Entity<Cliente>()
                .Property(p => p.Documento)
                .IsRequired()
                .HasColumnType("varchar(14)");

            modelBuilder.Entity<Cliente>()
                .Property(p => p.Telefone)
                .IsRequired()
                .HasColumnType("varchar(11)");

            modelBuilder.Entity<Cliente>()
                .ToTable("Clientes");

            base.OnModelCreating(modelBuilder);
        }
    }
}
