using Microsoft.AspNetCore.Mvc.Rendering;

namespace e_commerce_web_admin.ViewModels.Staff;

public sealed class StaffIndexViewModel
{
    public List<StaffRowViewModel> Staff { get; set; } = [];
    public string? Search { get; set; }
}

public sealed class StaffRowViewModel
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentStaff { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public sealed class StaffFormViewModel
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public List<string> SelectedRoles { get; set; } = [];
    public List<SelectListItem> RoleOptions { get; set; } = [];
    public bool IsCurrentStaff { get; set; }

    public bool IsCreate => Id == 0;
}
