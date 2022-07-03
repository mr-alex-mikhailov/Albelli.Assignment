using Microsoft.EntityFrameworkCore;
using Albelli.Assignment.Application.DataContext.Entities;

namespace Albelli.Assignment.Application.DataContext
{
    public class ApplicationDataContext : DbContext
    {
        public ApplicationDataContext(DbContextOptions<ApplicationDataContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderEntry> OrderEntries => Set<OrderEntry>();
        public DbSet<ProductType> ProductTypes => Set<ProductType>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductType>()
                .HasIndex(p => p.Code).IsUnique();
            modelBuilder.Entity<ProductType>()
                .Property(p => p.MaxAmountInGroup).HasDefaultValue(1);

            var productTypes = new[]
            {
                new ProductType { Id = 1, Code = "photoBook", WidthInBin = 19M, MaxAmountInGroup = 1 },
                new ProductType { Id = 2, Code = "calendar", WidthInBin = 10M, MaxAmountInGroup = 1 },
                new ProductType { Id = 3, Code = "canvas", WidthInBin = 16M, MaxAmountInGroup = 1 },
                new ProductType { Id = 4, Code = "cards", WidthInBin = 4.7M, MaxAmountInGroup = 1 },
                new ProductType { Id = 5, Code = "mug", WidthInBin = 94M, MaxAmountInGroup = 4 }
            };
            modelBuilder.Entity<ProductType>().HasData(productTypes);
        }
    }
}
