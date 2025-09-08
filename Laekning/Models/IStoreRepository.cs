namespace Laekning.Models {

    // Interface defining the contract for a store repository
    public interface IStoreRepository {

        // Provides a queryable collection of all products in the store
        IQueryable<Product> Products { get; }

        // Save changes made to an existing product
        void SaveProduct(Product p);

        // Add a new product to the store
        void CreateProduct(Product p);

        // Remove a product from the store
        void DeleteProduct(Product p);
    }
}
