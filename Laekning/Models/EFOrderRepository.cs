using Microsoft.EntityFrameworkCore;

namespace Laekning.Models {

    // Entity Framework implementation of the IOrderRepository interface
    public class EFOrderRepository : IOrderRepository {
        // EF Core DbContext for accessing orders and related entities
        private StoreDbContext context;

        // Constructor injects the DbContext dependency
        public EFOrderRepository(StoreDbContext ctx) {
            context = ctx;
        }

        // IQueryable of orders including order lines and associated products
        public IQueryable<Order> Orders => context.Orders
                                .Include(o => o.Lines)        // Include order lines
                                .ThenInclude(l => l.Product); // Include the products for each line

        // Save a new order or update an existing one
        public void SaveOrder(Order order) {
            // Attach products in order lines to the context to avoid duplicate inserts
            context.AttachRange(order.Lines.Select(l => l.Product));

            // If the order is new, add it to the context
            if (order.OrderID == 0) {
                context.Orders.Add(order);
            }

            // Commit changes to the database
            context.SaveChanges();
        }
    }
}
