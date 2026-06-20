using System.Security.Claims;
using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.ViewModels.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Roles", Permissions.View)]
public sealed class RolesController(
    RoleManager<IdentityRole<long>> roleManager,
    UserManager<Staff> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync();

        var rows = new List<RoleRowViewModel>();
        foreach (var role in roles)
        {
            var roleName = role.Name ?? string.Empty;
            var claims = await roleManager.GetClaimsAsync(role);
            var staffCount = string.IsNullOrWhiteSpace(roleName)
                ? 0
                : (await userManager.GetUsersInRoleAsync(roleName)).Count;

            rows.Add(new RoleRowViewModel
            {
                Id = role.Id.ToString(),
                Name = roleName,
                PermissionCount = claims.Count(claim => claim.Type == StaffClaimTypes.Permission),
                StaffCount = staffCount,
                CanDelete = !IsAdminRole(roleName) && staffCount == 0,
            });
        }

        return View(new RoleIndexViewModel { Roles = rows });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Roles", Permissions.Create)]
    public async Task<IActionResult> Create(RoleIndexViewModel viewModel)
    {
        var roleName = viewModel.NewRoleName?.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            TempData["Error"] = "Tên vai trò là bắt buộc.";
            return RedirectToAction(nameof(Index));
        }

        if (await roleManager.RoleExistsAsync(roleName))
        {
            TempData["Error"] = "Vai trò đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        var result = await roleManager.CreateAsync(new IdentityRole<long>(roleName));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"Đã tạo vai trò \"{roleName}\"."
            : string.Join(" ", result.Errors.Select(error => error.Description));

        return RedirectToAction(nameof(Index));
    }

    [RbacAuthorize("Roles", Permissions.Edit)]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var claims = await roleManager.GetClaimsAsync(role);
        return View(new RoleEditViewModel
        {
            Id = role.Id.ToString(),
            Name = role.Name ?? string.Empty,
            SelectedPermissions = claims
                .Where(claim => claim.Type == StaffClaimTypes.Permission)
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToList(),
            Modules = PermissionModules.All,
            PermissionActions = Permissions.All,
            ModuleNames = PermissionModules.DisplayNames,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Roles", Permissions.Edit)]
    public async Task<IActionResult> Edit(string id, RoleEditViewModel viewModel)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        viewModel.SelectedPermissions ??= [];
        var roleName = viewModel.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(roleName))
        {
            ModelState.AddModelError(nameof(viewModel.Name), "Tên vai trò là bắt buộc.");
        }
        else if (IsAdminRole(role.Name) && !IsAdminRole(roleName))
        {
            ModelState.AddModelError(nameof(viewModel.Name), "Không thể đổi tên vai trò Admin.");
        }

        if (!ModelState.IsValid)
        {
            await PrepareRoleEditViewModelAsync(viewModel, role);
            return View(viewModel);
        }

        role.Name = roleName;
        var updateResult = await roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            AddIdentityErrors(updateResult);
            await PrepareRoleEditViewModelAsync(viewModel, role);
            return View(viewModel);
        }

        var claimResult = await SyncPermissionClaimsAsync(role, viewModel.SelectedPermissions);
        if (!claimResult.Succeeded)
        {
            AddIdentityErrors(claimResult);
            await PrepareRoleEditViewModelAsync(viewModel, role);
            return View(viewModel);
        }

        await RefreshRoleMembersSecurityStampAsync(roleName);
        TempData["Success"] = $"Đã cập nhật vai trò \"{roleName}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Roles", Permissions.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var roleName = role.Name ?? string.Empty;
        var staffCount = string.IsNullOrWhiteSpace(roleName)
            ? 0
            : (await userManager.GetUsersInRoleAsync(roleName)).Count;
        if (IsAdminRole(roleName) || staffCount > 0)
        {
            TempData["Error"] = "Không thể xóa vai trò Admin hoặc vai trò đang có nhân sự.";
            return RedirectToAction(nameof(Index));
        }

        var claimResult = await RemovePermissionClaimsAsync(role);
        if (!claimResult.Succeeded)
        {
            TempData["Error"] = string.Join(" ", claimResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        var result = await roleManager.DeleteAsync(role);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"Đã xóa vai trò \"{roleName}\"."
            : string.Join(" ", result.Errors.Select(error => error.Description));

        return RedirectToAction(nameof(Index));
    }

    private async Task PrepareRoleEditViewModelAsync(RoleEditViewModel viewModel, IdentityRole<long> role)
    {
        var claims = await roleManager.GetClaimsAsync(role);
        viewModel.Id = role.Id.ToString();
        viewModel.Name = viewModel.Name?.Trim() ?? role.Name ?? string.Empty;
        viewModel.Modules = PermissionModules.All;
        viewModel.PermissionActions = Permissions.All;
        viewModel.ModuleNames = PermissionModules.DisplayNames;
        viewModel.SelectedPermissions = claims
            .Where(claim => claim.Type == StaffClaimTypes.Permission)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }

    private async Task<IdentityResult> SyncPermissionClaimsAsync(
        IdentityRole<long> role,
        IReadOnlyCollection<string> selectedPermissions)
    {
        var allowedPermissions = PermissionModules.All
            .SelectMany(module => Permissions.All.Select(permission => Permissions.Build(module, permission)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selected = selectedPermissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Where(allowedPermissions.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var removeResult = await RemovePermissionClaimsAsync(role);
        if (!removeResult.Succeeded)
        {
            return removeResult;
        }

        foreach (var permission in selected)
        {
            var addResult = await roleManager.AddClaimAsync(role, new Claim(StaffClaimTypes.Permission, permission));
            if (!addResult.Succeeded)
            {
                return addResult;
            }
        }

        return IdentityResult.Success;
    }

    private async Task<IdentityResult> RemovePermissionClaimsAsync(IdentityRole<long> role)
    {
        var claims = await roleManager.GetClaimsAsync(role);
        foreach (var claim in claims.Where(claim => claim.Type == StaffClaimTypes.Permission).ToList())
        {
            var result = await roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded)
            {
                return result;
            }
        }

        return IdentityResult.Success;
    }

    private async Task RefreshRoleMembersSecurityStampAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return;
        }

        foreach (var staff in await userManager.GetUsersInRoleAsync(roleName))
        {
            await userManager.UpdateSecurityStampAsync(staff);
        }
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static bool IsAdminRole(string? roleName) =>
        string.Equals(roleName, StaffRoleNames.Admin, StringComparison.OrdinalIgnoreCase);
}
