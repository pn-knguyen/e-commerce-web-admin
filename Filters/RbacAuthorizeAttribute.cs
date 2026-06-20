using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace e_commerce_web_admin.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RbacAuthorizeAttribute(string module, string permission) : System.Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var principal = context.HttpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            context.Result = BuildLoginRedirect(context);
            return;
        }

        var services = context.HttpContext.RequestServices;
        var userManager = services.GetRequiredService<UserManager<Staff>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<long>>>();
        var staff = await userManager.GetUserAsync(principal);

        if (staff is null || !staff.IsActive)
        {
            if (services.GetService<SignInManager<Staff>>() is { } signInManager)
            {
                await signInManager.SignOutAsync();
            }

            context.Result = BuildLoginRedirect(context);
            return;
        }

        if (await userManager.IsInRoleAsync(staff, StaffRoleNames.Admin))
        {
            return;
        }

        var permissionValue = Permissions.Build(module, permission);
        var roleNames = await userManager.GetRolesAsync(staff);

        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var claims = await roleManager.GetClaimsAsync(role);
            if (claims.Any(claim =>
                    claim.Type == StaffClaimTypes.Permission &&
                    string.Equals(claim.Value, permissionValue, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
        }

        context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }

    private static RedirectToActionResult BuildLoginRedirect(AuthorizationFilterContext context)
    {
        var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
        return new RedirectToActionResult("Login", "Account", new { returnUrl });
    }
}
