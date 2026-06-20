using System.Security.Claims;
using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.ViewModels.Staff;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Staff", Permissions.View)]
public sealed class StaffController(
    UserManager<Staff> userManager,
    RoleManager<IdentityRole<long>> roleManager) : Controller
{
    public async Task<IActionResult> Index(string? search = null)
    {
        var query = userManager.Users.AsNoTracking();
        var normalizedSearch = search?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(staff =>
                staff.UserName!.Contains(normalizedSearch) ||
                staff.Email!.Contains(normalizedSearch) ||
                staff.FullName.Contains(normalizedSearch) ||
                (staff.PhoneNumber != null && staff.PhoneNumber.Contains(normalizedSearch)));
        }

        var currentStaffId = GetCurrentStaffId();
        var staffList = await query
            .OrderByDescending(staff => staff.IsActive)
            .ThenBy(staff => staff.FullName)
            .ToListAsync();

        var rows = new List<StaffRowViewModel>();
        foreach (var staff in staffList)
        {
            rows.Add(new StaffRowViewModel
            {
                Id = staff.Id,
                UserName = staff.UserName ?? string.Empty,
                Email = staff.Email ?? string.Empty,
                FullName = staff.FullName,
                PhoneNumber = staff.PhoneNumber,
                IsActive = staff.IsActive,
                CreatedAt = staff.CreatedAt,
                IsCurrentStaff = currentStaffId == staff.Id,
                Roles = (await userManager.GetRolesAsync(staff)).ToList(),
            });
        }

        return View(new StaffIndexViewModel
        {
            Staff = rows,
            Search = normalizedSearch,
        });
    }

    [RbacAuthorize("Staff", Permissions.Create)]
    public async Task<IActionResult> Create()
    {
        var form = new StaffFormViewModel();
        await PrepareFormAsync(form);
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Staff", Permissions.Create)]
    public async Task<IActionResult> Create(StaffFormViewModel form)
    {
        NormalizeForm(form);
        await PrepareFormAsync(form);

        ValidateForm(form, isCreate: true);
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var staff = new Staff
        {
            UserName = form.UserName,
            Email = form.Email,
            FullName = form.FullName,
            PhoneNumber = form.PhoneNumber,
            IsActive = form.IsActive,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var createResult = await userManager.CreateAsync(staff, form.Password!);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(form);
        }

        var roleResult = await SyncRolesAsync(staff, form.SelectedRoles);
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            return View(form);
        }

        TempData["Success"] = $"Đã tạo nhân sự \"{staff.UserName}\".";
        return RedirectToAction(nameof(Index));
    }

    [RbacAuthorize("Staff", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id)
    {
        var staff = await userManager.FindByIdAsync(id.ToString());
        if (staff is null)
        {
            return NotFound();
        }

        var form = new StaffFormViewModel
        {
            Id = staff.Id,
            UserName = staff.UserName ?? string.Empty,
            Email = staff.Email ?? string.Empty,
            FullName = staff.FullName,
            PhoneNumber = staff.PhoneNumber,
            IsActive = staff.IsActive,
            SelectedRoles = (await userManager.GetRolesAsync(staff)).ToList(),
            IsCurrentStaff = GetCurrentStaffId() == staff.Id,
        };

        await PrepareFormAsync(form);
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Staff", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, StaffFormViewModel form)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        var staff = await userManager.FindByIdAsync(id.ToString());
        if (staff is null)
        {
            return NotFound();
        }

        NormalizeForm(form);
        form.IsCurrentStaff = GetCurrentStaffId() == staff.Id;
        await PrepareFormAsync(form);

        ValidateForm(form, isCreate: false);
        if (form.IsCurrentStaff && !form.IsActive)
        {
            ModelState.AddModelError(nameof(form.IsActive), "Không thể tự khóa tài khoản đang đăng nhập.");
        }

        if (ModelState.IsValid &&
            await WouldRemoveLastActiveAdminAsync(staff, form.SelectedRoles, form.IsCurrentStaff || form.IsActive))
        {
            ModelState.AddModelError(
                nameof(form.SelectedRoles),
                "Hệ thống phải còn ít nhất một nhân sự đang hoạt động có vai trò Admin.");
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        staff.UserName = form.UserName;
        staff.Email = form.Email;
        staff.FullName = form.FullName;
        staff.PhoneNumber = form.PhoneNumber;
        staff.IsActive = form.IsCurrentStaff || form.IsActive;
        staff.UpdatedAt = DateTime.UtcNow;

        var updateResult = await userManager.UpdateAsync(staff);
        if (!updateResult.Succeeded)
        {
            AddIdentityErrors(updateResult);
            return View(form);
        }

        if (!string.IsNullOrWhiteSpace(form.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(staff);
            var passwordResult = await userManager.ResetPasswordAsync(staff, token, form.Password);
            if (!passwordResult.Succeeded)
            {
                AddIdentityErrors(passwordResult);
                return View(form);
            }
        }

        var roleResult = await SyncRolesAsync(staff, form.SelectedRoles);
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            return View(form);
        }

        await userManager.UpdateSecurityStampAsync(staff);
        TempData["Success"] = $"Đã cập nhật nhân sự \"{staff.UserName}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Staff", Permissions.Edit)]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var staff = await userManager.FindByIdAsync(id.ToString());
        if (staff is null)
        {
            return NotFound();
        }

        if (GetCurrentStaffId() == staff.Id)
        {
            TempData["Error"] = "Không thể tự khóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        if (staff.IsActive &&
            await IsActiveAdminAsync(staff) &&
            !await HasAnotherActiveAdminAsync(staff.Id))
        {
            TempData["Error"] = "Không thể khóa admin cuối cùng đang hoạt động.";
            return RedirectToAction(nameof(Index));
        }

        staff.IsActive = !staff.IsActive;
        staff.UpdatedAt = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(staff);
        if (!updateResult.Succeeded)
        {
            TempData["Error"] = string.Join(" ", updateResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Index));
        }

        await userManager.UpdateSecurityStampAsync(staff);
        TempData["Success"] = staff.IsActive
            ? $"Đã kích hoạt nhân sự \"{staff.UserName}\"."
            : $"Đã tạm khóa nhân sự \"{staff.UserName}\".";

        return RedirectToAction(nameof(Index));
    }

    private async Task PrepareFormAsync(StaffFormViewModel form)
    {
        form.SelectedRoles ??= [];

        var selectedRoles = form.SelectedRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roles = await roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .Where(role => role != null)
            .ToListAsync();

        form.RoleOptions = roles
            .Select(role => new SelectListItem
            {
                Value = role,
                Text = role,
                Selected = selectedRoles.Contains(role),
            })
            .ToList();
    }

    private async Task<IdentityResult> SyncRolesAsync(Staff staff, IReadOnlyCollection<string> selectedRoles)
    {
        var availableRoles = await roleManager.Roles
            .AsNoTracking()
            .Select(role => role.Name!)
            .Where(role => role != null)
            .ToListAsync();
        var availableRoleSet = availableRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = selectedRoles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var invalidRole = selected.FirstOrDefault(role => !availableRoleSet.Contains(role));
        if (!string.IsNullOrWhiteSpace(invalidRole))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidRole",
                Description = $"Vai trò \"{invalidRole}\" không tồn tại.",
            });
        }

        var currentRoles = await userManager.GetRolesAsync(staff);
        var remove = currentRoles.Except(selected, StringComparer.OrdinalIgnoreCase).ToArray();
        var add = selected.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();

        if (remove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(staff, remove);
            if (!removeResult.Succeeded)
            {
                return removeResult;
            }
        }

        if (add.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(staff, add);
            if (!addResult.Succeeded)
            {
                return addResult;
            }
        }

        return IdentityResult.Success;
    }

    private void ValidateForm(StaffFormViewModel form, bool isCreate)
    {
        if (string.IsNullOrWhiteSpace(form.UserName))
        {
            ModelState.AddModelError(nameof(form.UserName), "Tên đăng nhập là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(form.Email))
        {
            ModelState.AddModelError(nameof(form.Email), "Email là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(form.FullName))
        {
            ModelState.AddModelError(nameof(form.FullName), "Họ tên là bắt buộc.");
        }

        if (isCreate && string.IsNullOrWhiteSpace(form.Password))
        {
            ModelState.AddModelError(nameof(form.Password), "Mật khẩu là bắt buộc khi tạo nhân sự.");
        }

        if (!string.IsNullOrWhiteSpace(form.Password) || !string.IsNullOrWhiteSpace(form.ConfirmPassword))
        {
            if (form.Password?.Length < 6)
            {
                ModelState.AddModelError(nameof(form.Password), "Mật khẩu tối thiểu 6 ký tự.");
            }

            if (!string.Equals(form.Password, form.ConfirmPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(form.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
            }
        }

        if (form.SelectedRoles.Count == 0)
        {
            ModelState.AddModelError(nameof(form.SelectedRoles), "Vui lòng chọn ít nhất một vai trò.");
        }
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static void NormalizeForm(StaffFormViewModel form)
    {
        form.UserName = form.UserName?.Trim() ?? string.Empty;
        form.Email = form.Email?.Trim() ?? string.Empty;
        form.FullName = form.FullName?.Trim() ?? string.Empty;
        form.PhoneNumber = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber.Trim();
        form.Password = string.IsNullOrWhiteSpace(form.Password) ? null : form.Password.Trim();
        form.ConfirmPassword = string.IsNullOrWhiteSpace(form.ConfirmPassword) ? null : form.ConfirmPassword.Trim();
        form.SelectedRoles ??= [];
        form.SelectedRoles = form.SelectedRoles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<bool> WouldRemoveLastActiveAdminAsync(
        Staff staff,
        IReadOnlyCollection<string> selectedRoles,
        bool nextIsActive)
    {
        if (!await IsActiveAdminAsync(staff))
        {
            return false;
        }

        var willRemainActiveAdmin = nextIsActive &&
            selectedRoles.Any(role => string.Equals(role, StaffRoleNames.Admin, StringComparison.OrdinalIgnoreCase));

        return !willRemainActiveAdmin && !await HasAnotherActiveAdminAsync(staff.Id);
    }

    private async Task<bool> IsActiveAdminAsync(Staff staff)
    {
        return staff.IsActive && await userManager.IsInRoleAsync(staff, StaffRoleNames.Admin);
    }

    private async Task<bool> HasAnotherActiveAdminAsync(long staffId)
    {
        var admins = await userManager.GetUsersInRoleAsync(StaffRoleNames.Admin);
        return admins.Any(staff => staff.Id != staffId && staff.IsActive);
    }

    private long? GetCurrentStaffId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out var id) ? id : null;
    }
}
