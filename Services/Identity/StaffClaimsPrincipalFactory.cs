using System.Security.Claims;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace e_commerce_web_admin.Services.Identity;

public sealed class StaffClaimsPrincipalFactory(
    UserManager<Staff> userManager,
    RoleManager<IdentityRole<long>> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<Staff, IdentityRole<long>>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Staff user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(AppClaimTypes.UserId, user.Id.ToString()));
        identity.AddClaim(new Claim(AppClaimTypes.FullName, user.FullName));

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            identity.AddClaim(new Claim(AppClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.AvatarImage))
        {
            identity.AddClaim(new Claim(AppClaimTypes.Avatar, user.AvatarImage));
        }

        return identity;
    }
}
