using Microsoft.AspNetCore.Mvc.RazorPages;
using Laekning.Models;
using System.Collections.Generic;

namespace Laekning.Pages
{
    public class SupportModel : PageModel
    {
        public List<SupportOption> SupportOptions { get; set; } = new List<SupportOption>
        {
            new SupportOption { Name = "FAQ", Description = "Find answers to frequently asked questions." },
            new SupportOption { Name = "Technical Support", Description = "Get help with technical issues and troubleshooting. \nTry our new chatbot by clicking the \"chatbot\" option in the features tab." },
            new SupportOption { Name = "Customer Service", Description = "Contact the customer service personnel for further assistance at mail jenishpokhreltechdemo@gmail.com" }
        };

        public string SelectedOption { get; set; }
        public string OptionDetails { get; set; }

        public void OnGet(string option)
        {
            var selected = SupportOptions.Find(o => o.Name == option);
            SelectedOption = selected?.Name ?? "Select a Support Option";
            OptionDetails = selected?.Description ?? "Please select a support option from the sidebar.";
        }
    }
}
