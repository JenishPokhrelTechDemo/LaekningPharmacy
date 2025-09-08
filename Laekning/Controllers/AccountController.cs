using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Laekning.Models.ViewModels;

namespace Laekning.Controllers {

    public class AccountController : Controller {
        // ASP.NET Core Identity user manager for handling user data
        private UserManager<IdentityUser> userManager;

        // ASP.NET Core Identity sign-in manager for handling login/logout
        private SignInManager<IdentityUser> signInManager;

        // Constructor: injects UserManager and SignInManager dependencies
        public AccountController(UserManager<IdentityUser> userMgr,
                SignInManager<IdentityUser> signInMgr) {
            userManager = userMgr;
            signInManager = signInMgr;
        }

        // GET: Display login page
        public ViewResult Login(string returnUrl) {
            return View(new LoginModel {
                Name = string.Empty, // Initialize username field
                Password = string.Empty, // Initialize password field
                ReturnUrl = returnUrl // Set redirect URL after successful login
            });
        }

        // POST: Handle login form submission
        [HttpPost]
        [ValidateAntiForgeryToken] // Protect against CSRF attacks
        public async Task<IActionResult> Login(LoginModel loginModel) {
            if (ModelState.IsValid) {
                // Try to find the user by username
                IdentityUser? user =
                    await userManager.FindByNameAsync(loginModel.Name);

                if (user != null) {
                    // Sign out any existing user session
                    await signInManager.SignOutAsync();

                    // Attempt password sign-in
                    if ((await signInManager.PasswordSignInAsync(user,
                        loginModel.Password, false, false)).Succeeded) {
                        // Redirect to ReturnUrl if specified, otherwise go to /Admin
                        return Redirect(loginModel?.ReturnUrl ?? "/Admin");
                    }
                }

                // If login fails, add a validation error
                ModelState.AddModelError("", "Invalid name or password");
            }

            // If validation fails, redisplay login form with errors
            return View(loginModel);
        }

        // GET: Logout the current user
        [Authorize] // Only accessible if user is logged in
        public async Task<RedirectResult> Logout(string returnUrl = "/") {
            // Sign out the current user
            await signInManager.SignOutAsync();

            // Redirect to the specified return URL (default is home page)
            return Redirect(returnUrl);
        }
    }
}
