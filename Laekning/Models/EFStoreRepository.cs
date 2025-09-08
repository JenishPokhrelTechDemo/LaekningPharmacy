namespace Laekning.Models {

    // Entity Framework implementation of the IStoreRepository interface
    public class EFStoreRepository : IStoreRepository {
        // EF Core DbContext for accessing products in the store
        private StoreDbContext context;

        // Constructor injects the DbContext dependency
        public EFStoreRepository(StoreDbContext ctx) {
            context = ctx;
        }

        // IQueryable of all products in the database
        public IQueryable<Product> Products => context.Products;

        // Add a new product to the database
        public void CreateProduct(Product p) {
            context.Add(p);      // Mark the product as added
            context.SaveChanges(); // Commit changes to the database
        }

        // Remove an existing product from the database
        public void DeleteProduct(Product p) {
            context.Remove(p);    // Mark the product for deletion
            context.SaveChanges(); // Commit changes to the database
        }
