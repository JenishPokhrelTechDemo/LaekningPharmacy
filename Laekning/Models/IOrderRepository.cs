namespace Laekning.Models {

    // Interface defining the contract for an order repository
    public interface IOrderRepository {
        // Provides a queryable collection of all orders
        IQueryable<Order> Orders { get; }

        // Saves a new order or updates an existing order
        void SaveOrder(Order order);
    }
}
