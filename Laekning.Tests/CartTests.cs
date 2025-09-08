using System.Linq;
using Laekning.Models; // Your application's models (Cart, Product, CartLine)
using Xunit;           // xUnit testing framework

namespace Laekning.Tests;

// Unit tests for the Cart class
public class CartTests
{
    [Fact]
    public void Can_Add_New_Lines()
    {
        // Arrange: create sample products
        Product p1 = new Product { ProductID = 1, Name = "P1" };
        Product p2 = new Product { ProductID = 2, Name = "P2" };

        // Create an empty cart
        Cart target = new Cart();

        // Act: add products to the cart
        target.AddItem(p1, 1);
        target.AddItem(p2, 1);

        // Convert cart lines to array for assertions
        CartLine[] results = target.Lines.ToArray();

        // Assert: cart has 2 lines with correct products
        Assert.Equal(2, results.Length);
        Assert.Equal(p1, results[0].Product);
        Assert.Equal(p2, results[1].Product);
    }

    [Fact]
    public void Can_Add_Quantity_For_Existing_Lines()
    {
        // Arrange: create sample products
        Product p1 = new Product { ProductID = 1, Name = "P1" };
        Product p2 = new Product { ProductID = 2, Name = "P2" };

        // Create empty cart
        Cart target = new Cart();

        // Act: add products, then add more quantity to an existing line
        target.AddItem(p1, 1);
        target.AddItem(p2, 1);
        target.AddItem(p1, 10); // increase quantity for p1

        // Sort cart lines by ProductID for consistent order
        CartLine[] results = (target.Lines ?? new())
            .OrderBy(c => c.Product.ProductID).ToArray();

        // Assert: check that quantities updated correctly
        Assert.Equal(2, results.Length);   // Only two unique products
        Assert.Equal(11, results[0].Quantity); // p1 quantity summed
        Assert.Equal(1, results[1].Quantity);  // p2 quantity unchanged
    }

    [Fact]
    public void Can_Remove_Line()
    {
        // Arrange: create sample products
        Product p1 = new Product { ProductID = 1, Name = "P1" };
        Product p2 = new Product { ProductID = 2, Name = "P2" };
        Product p3 = new Product { ProductID = 3, Name = "P3" };

        // Create a cart and add products
        Cart target = new Cart();
        target.AddItem(p1, 1);
        target.AddItem(p2, 3);
        target.AddItem(p3, 5);
        target.AddItem(p2, 1); // increase quantity of p2

        // Act: remove p2 completely from cart
        target.RemoveLine(p2);

        // Assert: p2 no longer exists in cart
        Assert.Empty(target.Lines.Where(c => c.Product == p2));
        // Total remaining lines = 2 (p1 and p3)
        Assert.Equal(2, target.Lines.Count());
    }

    [Fact]
    public void Calculate_Cart_Total()
    {
        // Arrange: create products with prices
        Product p1 = new Product { ProductID = 1, Name = "P1", Price = 100M };
        Product p2 = new Product { ProductID = 2, Name = "P2", Price = 50M };

        Cart target = new Cart();

        // Act: add items to cart
        target.AddItem(p1, 1); // 100
        target.AddItem(p2, 1); // 50
        target.AddItem(p1, 3); // +300
        decimal result = target.ComputeTotalValue();

        // Assert: total value should sum correctly
        Assert.Equal(450M, result);
    }

    [Fact]
    public void Can_Clear_Contents()
    {
        // Arrange: create sample products
        Product p1 = new Product { ProductID = 1, Name = "P1", Price = 100M };
        Product p2 = new Product { ProductID = 2, Name = "P2", Price = 50M };

        Cart target = new Cart();

        // Act: add items then clear the cart
        target.AddItem(p1, 1);
        target.AddItem(p2, 1);
        target.Clear();

        // Assert: cart should be empty after clearing
        Assert.Empty(target.Lines);
    }
}
