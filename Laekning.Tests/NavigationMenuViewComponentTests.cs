using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq; // For mocking dependencies
using Laekning.Components; // NavigationMenuViewComponent
using Laekning.Models; // Product, IStoreRepository
using Xunit; // xUnit testing framework

namespace Laekning.Tests;

public class NavigationMenuViewComponentTests{

    [Fact]
    public void Can_Select_Categories(){
        // Arrange: create a mock repository with sample products in various categories
        Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
        mock.Setup(m => m.Products).Returns((new Product[] {
            new Product {ProductID = 1, Name = "P1", Category = "Apples"},
            new Product {ProductID = 2, Name = "P2", Category = "Apples"},
            new Product {ProductID = 3, Name = "P3", Category = "Plums"},
            new Product {ProductID = 4, Name = "P4", Category = "Oranges"},
        }).AsQueryable<Product>());

        // Create the view component with the mock repository
        NavigationMenuViewComponent target = new NavigationMenuViewComponent(mock.Object);

        // Act: invoke the view component to get the category list
        string[] results = ((IEnumerable<string>?)(target.Invoke() as ViewViewComponentResult)?.ViewData?.Model 
                            ?? Enumerable.Empty<string>()).ToArray();

        // Assert: check that categories are returned in sorted order without duplicates
        Assert.True(Enumerable.SequenceEqual(new string[]{"Apples", "Oranges", "Plums"}, results));
    }

    [Fact]
    public void Indicates_Selected_Category(){
        // Arrange: define the category to simulate being selected
        string categoryToSelect = "Apples";

        // Mock repository with sample products
        Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
        mock.Setup(m => m.Products).Returns((new Product[] {
            new Product {ProductID = 1, Name = "P1", Category = "Apples"},
            new Product {ProductID = 4, Name = "P2", Category = "Oranges"},
        }).AsQueryable<Product>());

        // Create the view component
        NavigationMenuViewComponent target = new NavigationMenuViewComponent(mock.Object);

        // Setup ViewComponentContext and RouteData to simulate selected category
        target.ViewComponentContext = new ViewComponentContext {
            ViewContext = new ViewContext{
                RouteData = new Microsoft.AspNetCore.Routing.RouteData()
            }
        };
        target.RouteData.Values["category"] = categoryToSelect;

        // Act: invoke the view component and retrieve the selected category from ViewData
        string? result = (string?)(target.Invoke()
            as ViewViewComponentResult)?.ViewData?["SelectedCategory"];

        // Assert: ensure the component correctly indicates the selected category
        Assert.Equal(categoryToSelect, result);
    }
}
