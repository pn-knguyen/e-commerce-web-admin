namespace e_commerce_web_admin.ViewModels.Roles;

public sealed class RoleIndexViewModel
{
    public List<RoleRowViewModel> Roles { get; set; } = [];
    public string? NewRoleName { get; set; }
}

public sealed class RoleRowViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
    public int StaffCount { get; set; }
    public bool CanDelete { get; set; }
}

public sealed class RoleEditViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> SelectedPermissions { get; set; } = [];
    public string[] Modules { get; set; } = [];
    public string[] PermissionActions { get; set; } = [];
    public IReadOnlyDictionary<string, string> ModuleNames { get; set; } = new Dictionary<string, string>();
}
