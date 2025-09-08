using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Laekning.Models {

    // Custom Identity DbContext for ASP.NET Core Identity
    // Handles users, roles, and related identity tables
    public class AppIdentityDbContext : IdentityDbContext<IdentityUser> {

        // Constructor accepts DbContextOptions and passes them to the base IdentityDbContext
        public AppIdentityDbContext(
                DbContextOptions<AppIdentityDbContext> options)
            : base(options) { }
    }
}
