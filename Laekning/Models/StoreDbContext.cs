using Microsoft.EntityFrameworkCore;

namespace Laekning.Models {

    // Represents the Entity Framework database context for the store
    public class StoreDbContext : DbContext {

        // Constructor accepting DbContext options and passing them to the base class
        public StoreDbContext(DbContextOptions<StoreDbContext> options)
            : base(options) { }

        // DbSet representing all products in the database
        public DbSet<Product> Products => Set<Product>();

        // DbSet representing all orders in the database
        public DbSet<Order> Orders => Set<Order>();
    }
}
