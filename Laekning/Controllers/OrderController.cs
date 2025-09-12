using Microsoft.AspNetCore.Mvc;
using Laekning.Models;
using Laekning.Services;
using System.Text.Json;

namespace Laekning.Controllers {
    public class OrderController : Controller {

        // Repository for saving and retrieving orders
        public IOrderRepository repository;

        // Shopping cart instance for current user/session
        private Cart cart;

        // EventHubSender for publishing order events
        private readonly EventHubSender eventHubSender;

        // Dependencies are injected via constructor
        public OrderController(IOrderRepository reposervice, Cart cartService, EventHubSender hubSender) {
            repository = reposervice;
            cart = cartService;
            eventHubSender = hubSender;
        }

        // Display the checkout page with a new empty order
        public ViewResult Checkout() => View(new Order());

        // Handle checkout form submission
        [HttpPost]
        public async Task<IActionResult> Checkout(Order order) {
            // If cart is empty, add a validation error
            if (cart.Lines.Count() == 0) {
                ModelState.AddModelError("", "Sorry, your cart is empty!");
            }

            // Process the order if all inputs are valid
            if (ModelState.IsValid) {
                // Copy cart lines into the order
                order.Lines = cart.Lines.ToArray();

                // Save order to repository
                repository.SaveOrder(order);

                // Prepare order event payload
                var orderEvent = new {
                    EventType = "OrderPlaced",
                    OrderId = order.OrderID,
                    Customer = order.Name,
                    ItemCount = order.Lines.Count,
                    OrderDate = order.OrderDate,
                    GiftWrap = order.GiftWrap
                };

                // Serialize payload and send to Event Hub
                string payload = JsonSerializer.Serialize(orderEvent);
                await eventHubSender.SendAsync(payload);

                // Clear the cart after order completion
                cart.Clear();

                // Redirect user to the "Completed" page with order ID
                return RedirectToPage("/Completed",
                    new { orderId = order.OrderID });
            } else {
                // If validation fails, redisplay checkout form
                return View();
            }
        }
    }
}
