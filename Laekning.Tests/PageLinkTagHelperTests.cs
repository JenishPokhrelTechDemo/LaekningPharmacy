using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq; // For mocking dependencies
using Laekning.Infrastructure; // PageLinkTagHelper
using Laekning.Models.ViewModels; // PagingInfo
using Xunit; // xUnit testing framework

namespace Laekning.Tests {

    public class PageLinkTagHelperTests {

        [Fact]
        public void Can_Generate_Page_Links() {
            // Arrange

            // Mock IUrlHelper to simulate URL generation for pages
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.SetupSequence(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("Test/Page1") // First call returns Page1
                .Returns("Test/Page2") // Second call returns Page2
                .Returns("Test/Page3"); // Third call returns Page3

            // Mock IUrlHelperFactory to return our mocked IUrlHelper
            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            // Mock ViewContext (required by TagHelper)
            var viewContext = new Mock<ViewContext>();

            // Create the PageLinkTagHelper instance with sample PagingInfo
            PageLinkTagHelper helper = new PageLinkTagHelper(urlHelperFactory.Object) {
                PageModel = new PagingInfo {
                    CurrentPage = 2,   // Set current page for highlighting (not tested here)
                    TotalItems = 28,   // Total items in the list
                    ItemsPerPage = 10  // Number of items per page
                },
                ViewContext = viewContext.Object, // Required property for TagHelper
                PageAction = "Test"               // Action name used in URL generation
            };

            // Setup TagHelperContext: contains attributes, items, and unique ID (not used here)
            TagHelperContext ctx = new TagHelperContext(
                new TagHelperAttributeList(), 
                new Dictionary<object, object>(), 
                "");

            // Setup TagHelperOutput: the output object that will be modified by the TagHelper
            var content = new Mock<TagHelperContent>(); // Mocked inner content
            TagHelperOutput output = new TagHelperOutput(
                "div",                       // Tag name
                new TagHelperAttributeList(),// Attributes
                (cache, encoder) => Task.FromResult(content.Object) // Content delegate
            );

            // Act
            // Process generates the pagination links and writes to output.Content
            helper.Process(ctx, output);

            // Assert
            // Verify that output contains the expected HTML for 3 page links
            Assert.Equal(
                @"<a href=""Test/Page1"">1</a>"
                + @"<a href=""Test/Page2"">2</a>"
                + @"<a href=""Test/Page3"">3</a>",
                output.Content.GetContent()
            );
        }
    }
}
