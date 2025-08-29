using Microsoft.AspNetCore.Mvc;
using Laekning.Models;
using Laekning.Services;
using System.Text.Json;

namespace Laekning.Controllers{
	public class OrderController : Controller{
		
		public IOrderRepository repository;
		private Cart cart;
		//private readonly EventHubSender eventHubSender;
		
		public OrderController(IOrderRepository reposervice, Cart cartService /*EventHubSender hubSender*/){
			repository = reposervice;
			cart = cartService;
			//eventHubSender = hubSender;
			
		}
		
		public ViewResult Checkout() => View(new Order());
		
		[HttpPost]
		public async Task<IActionResult> Checkout(Order order) {
			if(cart.Lines.Count() == 0){
				ModelState.AddModelError("","Sorry, your cart is empty!");
			}
			if(ModelState.IsValid){
				order.Lines = cart.Lines.ToArray();
				repository.SaveOrder(order);
				
				 /* Fire Event: Order Placed
                var orderEvent = new {
                    EventType = "OrderPlaced",
                    OrderId = order.OrderID,
                    Customer = order.Name,
                    ItemCount = order.Lines.Count,
                    OrderDate = order.OrderDate,
                    GiftWrap = order.GiftWrap
                };

                object payload = JsonSerializer.Serialize(orderEvent);
                await eventHubSender.SendAsync(payload);*/
				
				cart.Clear();
				return RedirectToPage("/Completed", 
					new { orderId = order.OrderID });
			}else{
				return View();
			}
		}
	}
}