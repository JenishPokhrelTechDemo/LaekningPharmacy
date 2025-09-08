using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Laekning.Models.ViewModels;

namespace Laekning.Infrastructure {

    // Custom TagHelper to generate pagination links for a list of items
    [HtmlTargetElement("div", Attributes = "page-model")]
    public class PageLinkTagHelper : TagHelper {
        // Factory to create URL helpers for generating links
        private IUrlHelperFactory urlHelperFactory;

        // Constructor injects IUrlHelperFactory
        public PageLinkTagHelper(IUrlHelperFactory helperFactory) {
            urlHelperFactory = helperFactory;
        }

        // Context of the current view; not set via HTML attributes
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContext { get; set; }

        // Paging information for the current list
        public PagingInfo? PageModel { get; set; }

        // The action to generate URLs for
        public string? PageAction { get; set; }

        // Additional route values for generating page URLs, e.g., category filters
        [HtmlAttributeName(DictionaryAttributePrefix = "page-url-")]
        public Dictionary<string, object> PageUrlValues { get; set; }
            = new Dictionary<string, object>();

        // Enable CSS classes on pagination links
        public bool PageClassesEnabled { get; set; } = false;

        // Base CSS class for all links
        public string PageClass { get; set; } = String.Empty;

        // CSS class for normal (unselected) page links
        public string PageClassNormal { get; set; } = String.Empty;

        // CSS class for the currently selected page link
        public string PageClassSelected { get; set; } = String.Empty;

        // Main method that runs when the tag helper processes the element
        public override void Process(TagHelperContext context,
                TagHelperOutput output) {
            // Ensure ViewContext and PageModel are available
            if (ViewContext != null && PageModel != null) {
                // Create a URL helper for generating links
                IUrlHelper urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);

                // Container div to hold all pagination links
                TagBuilder result = new TagBuilder("div");

                // Loop through each page and generate a link
                for (int i = 1; i <= PageModel.TotalPages; i++) {
                    TagBuilder tag = new TagBuilder("a");

                    // Set the current page number as a route value
                    PageUrlValues["productPage"] = i;

                    // Generate the href attribute for the link
                    tag.Attributes["href"] = urlHelper.Action(PageAction,
                        PageUrlValues);

                    // Apply CSS classes if enabled
                    if (PageClassesEnabled) {
                        tag.AddCssClass(PageClass);
                        tag.AddCssClass(i == PageModel.CurrentPage
                            ? PageClassSelected : PageClassNormal);
                    }

                    // Set link text to the page number
                    tag.InnerHtml.Append(i.ToString());

                    // Add link to the container div
                    result.InnerHtml.AppendHtml(tag);
                }

                // Output the generated HTML to the Razor page
                output.Content.AppendHtml(result.InnerHtml);
            }
        }
    }
}
