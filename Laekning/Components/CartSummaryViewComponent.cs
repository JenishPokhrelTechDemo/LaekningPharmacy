using Microsoft.AspNetCore.Mvc;
using Laekning.Models;

namespace Laekning.Components{
	public class CartSummaryViewComponent : ViewComponent{
		private Cart cart;
		
		public CartSummaryViewComponent(Cart cartSummary){
			cart = cartSummary;
		}
		
		public IViewComponentResult Invoke(){
			return View(cart);
		}
	}
}