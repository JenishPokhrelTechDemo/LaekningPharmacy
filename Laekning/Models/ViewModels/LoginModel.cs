using System.ComponentModel.DataAnnotations;

namespace Laekning.Models.ViewModels {

    // ViewModel used for login form
    public class LoginModel {

        // Username or account name entered by the user
        public required string Name { get; set; }

        // Password entered by the user
        public required string Password { get; set; }

        // URL to redirect to after successful login (defaults to home page)
        public string ReturnUrl { get; set; } = "/";
    }
}
