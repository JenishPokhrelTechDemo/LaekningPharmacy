using Microsoft.AspNetCore.Mvc.RazorPages;
using Laekning.Models;
using System.Collections.Generic;

namespace Laekning.Pages
{
    // Razor Page model for the Support page
    public class SupportModel : PageModel
    {
        // List of available support options displayed in the sidebar
        public List<SupportOption> SupportOptions { get; set; } = new List<SupportOption>
        {
            // FAQ option with description
            new SupportOption { 
                Name = "FAQ", 
                Description = "Find answers to frequently asked questions." 
            },
            // Technical Support option, includes reference to chatbot
            new SupportOption { 
                Name = "Technical Support", 
                Description = "Get help with technical issues and troubleshooting. \nTry our new chatbot by clicking the \"chatbot\" option in the features tab." 
            },
            // Customer Service option with email contact
            new SupportOption { 
                Name = "Customer Service", 
                Description = "Contact the customer service personnel for further assistance at mail jenishpokhreltechdemo@gmail.com" 
            }
        };

        // Currently selected support option title
        public string SelectedOption { get; set; }

        // Detailed description for the selected option
        public string OptionDetails { get; set; }

        // Handles GET requests to the page
        // Accepts optional "option" parameter from query string to select a specific support option
        public void OnGet(string option)
        {
            // Find the support option matching the provided name
            var selected = SupportOptions.Find(o => o.Name == option);

            // Set SelectedOption to the option name or default text if not found
            SelectedOption = selected?.Name ?? "Select a Support Option";

            // Set OptionDetails to the option description or default message if not found
            OptionDetails = selected?.Description ?? "Please select a support option from the sidebar.";
        }
    }
}
