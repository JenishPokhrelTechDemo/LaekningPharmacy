using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Moq; // For mocking dependencies
using Laekning.Controllers; // HomeController
using Laekning.Models; // Product, IStoreRepository
using Laekning.Models.ViewModels; // ProductsListViewModel, PagingInfo
using Xunit; // xUnit testing framework

namespace Laekning.Tests {

    // Unit tests for HomeController
    public class HomeControllerTests {

        [Fact]
        public void Redirects_To_Recommendations_When_No_Category() {
            // Arrange: create a mock repository with sample products
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
                new Product {ProductID = 1, Name = "P1"},
                new Product {ProductID = 2, Name = "P2"}
            }).AsQueryable<Product>());

            HomeController controller = new HomeController(mock.Object);

            // Act: call Index with null category
            var result = controller.Index(null);

            // Assert: check that it redirects to /Recommendations
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Recommendations", redirectResult.PageName);
        }

        [Fact]
        public void Can_Paginate_By_Category() {
            // Arrange: mock repository with 5 products in same category
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
                new Product {ProductID = 1, Name = "P1", Category="Cat1"},
                new Product {ProductID = 2, Name = "P2", Category="Cat1"},
                new Product {ProductID = 3, Name = "P3", Category="Cat1"},
                new Product {ProductID = 4, Name = "P4", Category="Cat1"},
                new Product {ProductID = 5, Name = "P5", Category="Cat1"}
            }).AsQueryable<Product>());

            HomeController controller = new HomeController(mock.Object);
            controller.PageSize = 3; // page size = 3

            // Act: request second page for category "Cat1"
            ProductsListViewModel result =
                (controller.Index("Cat1", 2) as ViewResult)?.ViewData.Model as ProductsListViewModel
                    ?? new();

            // Assert: second page should contain last 2 products
            Product[] prodArray = result.Products.ToArray();
            Assert.True(prodArray.Length == 2);
            Assert.Equal("P4", prodArray[0].Name);
            Assert.Equal("P5", prodArray[1].Name);
        }

        [Fact]
        public void Can_Send_Pagination_View_Model() {
            // Arrange: same mock with 5 products, page size = 3
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
                new Product {ProductID = 1, Name = "P1", Category="Cat1"},
                new Product {ProductID = 2, Name = "P2", Category="Cat1"},
                new Product {ProductID = 3, Name = "P3", Category="Cat1"},
                new Product {ProductID = 4, Name = "P4", Category="Cat1"},
                new Product {ProductID = 5, Name = "P5", Category="Cat1"}
            }).AsQueryable<Product>());

            HomeController controller = new HomeController(mock.Object) { PageSize = 3 };

            // Act: get second page for "Cat1"
            ProductsListViewModel result =
                (controller.Index("Cat1", 2) as ViewResult)?.ViewData.Model as ProductsListViewModel
                    ?? new();

            // Assert: check pagination info
            PagingInfo pageInfo = result.PagingInfo;
            Assert.Equal(2, pageInfo.CurrentPage); // current page = 2
            Assert.Equal(3, pageInfo.ItemsPerPage); // items per page = 3
            Assert.Equal(5, pageInfo.TotalItems); // total items = 5
            Assert.Equal(2, pageInfo.TotalPages); // total pages = 2
        }

        [Fact]
        public void Can_Filter_Products() {
            // Arrange: mock repository with multiple categories
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
                new Product {ProductID = 1, Name = "P1", Category="Cat1"},
                new Product {ProductID = 2, Name = "P2", Category="Cat2"},
                new Product {ProductID = 3, Name = "P3", Category="Cat1"},
                new Product {ProductID = 4, Name = "P4", Category="Cat2"},
                new Product {ProductID = 5, Name = "P5", Category="Cat3"}
            }).AsQueryable<Product>());

            HomeController controller = new HomeController(mock.Object);
            controller.PageSize = 4; // default page size

            // Act: filter products by category "Cat2"
            Product[] result =
                ((controller.Index("Cat2", 1) as ViewResult)?.ViewData.Model as ProductsListViewModel
                    ?? new()).Products.ToArray();

            // Assert: only products from Cat2 are returned
            Assert.Equal(2, result.Length);
            Assert.True(result[0].Name == "P2" && result[0].Category == "Cat2");
            Assert.True(result[1].Name == "P4" && result[1].Category == "Cat2");
        }

        [Fact]
        public void Generate_Category_Specific_Product_Count() {
            // Arrange: repository with multiple categories
            Mock<IStoreRepository> mock = new Mock<IStoreRepository>();
            mock.Setup(m => m.Products).Returns((new Product[] {
                new Product {ProductID = 1, Name = "P1", Category = "Cat1"},
                new Product {ProductID = 2, Name = "P2", Category = "Cat2"},
                new Product {ProductID = 3, Name = "P3", Category = "Cat1"},
                new Product {ProductID = 4, Name = "P4", Category = "Cat2"},
                new Product {ProductID = 5, Name = "P5", Category = "Cat3"}
            }).AsQueryable<Product>());

            HomeController target = new HomeController(mock.Object);
            target.PageSize = 3;

            // Helper function to extract ViewModel from ViewResult
            Func<ViewResult?, ProductsListViewModel?> GetModel = result
                => result?.ViewData?.Model as ProductsListViewModel;

            // Act: get total items for each category
            int? res1 = GetModel(target.Index("Cat1") as ViewResult)?.PagingInfo.TotalItems;
            int? res2 = GetModel(target.Index("Cat2") as ViewResult)?.PagingInfo.TotalItems;
            int? res3 = GetModel(target.Index("Cat3") as ViewResult)?.PagingInfo.TotalItems;

            // Assert: verify correct counts per category
            Assert.Equal(2, res1); // Cat1 has 2 products
            Assert.Equal(2, res2); // Cat2 has 2 products
            Assert.Equal(1, res3); // Cat3 has 1 product
        }
    }
}
