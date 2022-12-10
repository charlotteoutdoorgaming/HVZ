using HVZ.Web.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace HVZ.Web.Identity
{
    public class ApplicationClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
    {
        public ApplicationClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            IOptions<IdentityOptions> options
        ) : base(userManager, options)
        {

        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            // Claims that we will likely need
            identity.AddClaim(new Claim("DatabaseId", user.DatabaseId));
            identity.AddClaim(new Claim("FirstName", user.FirstName));
            identity.AddClaim(new Claim("LastName", user.LastName));

            return identity;
        }
    }
}