namespace Laekning.Models {

    // Represents a support option that can be shown to users
    public class SupportOption {

        // Name of the support option (e.g., "FAQ", "Contact Us")
        public string Name { get; set; }

        // Description explaining what this support option does
        public string Description { get; set; }

        // Optional question or prompt associated with the support option
        // Initialized to a newline by default
        public string Question { get; set; } = "\n";
    }
}
