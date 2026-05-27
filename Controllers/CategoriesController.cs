using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace e_commerce_web_admin.Controllers;

public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ══════════════════════════════════════════════════════════════════════
    // INDEX — Danh sách dạng cây
    // ══════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        const int pageSize = 50; // tăng lên vì tree cần hiển thị đầy đủ

        // 1. Lấy toàn bộ categories kèm quan hệ
        var allQuery = _db.Categories
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .Include(c => c.Products)
            .AsNoTracking();

        // 2. Lọc theo status
        if (status == "active")
            allQuery = allQuery.Where(c => c.IsActive);
        else if (status == "inactive")
            allQuery = allQuery.Where(c => !c.IsActive);

        // 3. Tìm kiếm
        if (!string.IsNullOrWhiteSpace(search))
            allQuery = allQuery.Where(c => c.Name.Contains(search) || c.Slug.Contains(search));

        var allCats = await allQuery.ToListAsync();

        // 4. Build tree-ordered list bằng DFS
        //    → mỗi cha theo sau ngay bởi các con của nó (đệ quy)
        var ordered = new List<(Category Cat, int Depth)>();
        BuildDfsOrder(allCats, parentId: null, depth: 0, ordered);

        int total = ordered.Count;

        // 5. Phân trang
        var paged = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 6. Map sang ViewModel
        var rows = paged.Select(entry => new CategoryRowViewModel
        {
            Id           = entry.Cat.Id,
            Name         = entry.Cat.Name,
            Slug         = entry.Cat.Slug,
            ParentId     = entry.Cat.ParentId,
            ParentName   = entry.Cat.Parent?.Name ?? string.Empty,
            Position     = entry.Cat.Position,
            IsActive     = entry.Cat.IsActive,
            ProductCount = entry.Cat.Products.Count,
            ChildCount   = entry.Cat.Children.Count,
            Depth        = entry.Depth,
            CreatedAt    = entry.Cat.CreatedAt,
        }).ToList();

        var vm = new CategoryIndexViewModel
        {
            Categories = rows,
            Search     = search,
            Status     = status,
            Page       = page,
            PageSize   = pageSize,
            TotalCount = total,
        };

        return View(vm);
    }

    /// <summary>DFS traversal: cha → con ngay bên dưới theo Position rồi Name.</summary>
    private static void BuildDfsOrder(
        List<Category> all,
        long? parentId,
        int depth,
        List<(Category, int)> result)
    {
        var children = all
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.Position)
            .ThenBy(c => c.Name);

        foreach (var cat in children)
        {
            result.Add((cat, depth));
            BuildDfsOrder(all, cat.Id, depth + 1, result);
        }
    }


    // ══════════════════════════════════════════════════════════════════════
    // CREATE
    // ══════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> Create()
    {
        var vm = new CategoryFormViewModel
        {
            IsActive      = true,
            Position      = await _db.Categories.CountAsync() + 1,
            ParentOptions = await BuildParentOptionsAsync(excludeId: null),
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel vm)
    {
        // Slug auto nếu trống
        if (string.IsNullOrWhiteSpace(vm.Slug))
            vm.Slug = GenerateSlug(vm.Name);

        // Validate slug unique
        if (await _db.Categories.AnyAsync(c => c.Slug == vm.Slug))
            ModelState.AddModelError(nameof(vm.Slug), "Slug đã tồn tại, hãy dùng slug khác.");

        // Tránh chọn chính mình làm cha
        if (vm.ParentId.HasValue && vm.ParentId == vm.Id)
            ModelState.AddModelError(nameof(vm.ParentId), "Không thể chọn chính mình làm danh mục cha.");

        if (!ModelState.IsValid)
        {
            vm.ParentOptions = await BuildParentOptionsAsync(excludeId: null);
            return View(vm);
        }

        var entity = new Category
        {
            Name        = vm.Name.Trim(),
            Slug        = vm.Slug.Trim(),
            ParentId    = vm.ParentId,
            Description = vm.Description?.Trim(),
            ImagePath   = string.IsNullOrWhiteSpace(vm.ImagePath) ? null : vm.ImagePath.Trim(),
            Position    = vm.Position,
            IsActive    = vm.IsActive,
            CreatedAt   = DateTime.UtcNow,
        };

        _db.Categories.Add(entity);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo danh mục \"{entity.Name}\" thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ══════════════════════════════════════════════════════════════════════
    // EDIT
    // ══════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> Edit(long id)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity == null) return NotFound();

        var vm = new CategoryFormViewModel
        {
            Id          = entity.Id,
            Name        = entity.Name,
            Slug        = entity.Slug,
            ParentId    = entity.ParentId,
            Description = entity.Description,
            ImagePath   = entity.ImagePath,
            Position    = entity.Position,
            IsActive    = entity.IsActive,
            ParentOptions = await BuildParentOptionsAsync(excludeId: id),
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, CategoryFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        var entity = await _db.Categories.FindAsync(id);
        if (entity == null) return NotFound();

        // Slug auto nếu trống
        if (string.IsNullOrWhiteSpace(vm.Slug))
            vm.Slug = GenerateSlug(vm.Name);

        // Validate slug unique (trừ chính nó)
        if (await _db.Categories.AnyAsync(c => c.Slug == vm.Slug && c.Id != id))
            ModelState.AddModelError(nameof(vm.Slug), "Slug đã tồn tại, hãy dùng slug khác.");

        // Tránh chọn chính mình làm cha, hoặc chọn danh mục con làm cha
        if (vm.ParentId.HasValue)
        {
            if (vm.ParentId == id)
                ModelState.AddModelError(nameof(vm.ParentId), "Không thể chọn chính mình làm danh mục cha.");
            else if (await IsDescendantAsync(ancestorId: id, candidateId: vm.ParentId.Value))
                ModelState.AddModelError(nameof(vm.ParentId), "Không thể chọn danh mục con làm danh mục cha.");
        }

        if (!ModelState.IsValid)
        {
            vm.ParentOptions = await BuildParentOptionsAsync(excludeId: id);
            return View(vm);
        }

        entity.Name        = vm.Name.Trim();
        entity.Slug        = vm.Slug.Trim();
        entity.ParentId    = vm.ParentId;
        entity.Description = vm.Description?.Trim();
        entity.ImagePath   = string.IsNullOrWhiteSpace(vm.ImagePath) ? null : vm.ImagePath.Trim();
        entity.Position    = vm.Position;
        entity.IsActive    = vm.IsActive;
        entity.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật danh mục \"{entity.Name}\" thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ══════════════════════════════════════════════════════════════════════
    // DELETE
    // ══════════════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var entity = await _db.Categories
            .Include(c => c.Children)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity == null) return NotFound();

        // Kiểm tra ràng buộc
        if (entity.Children.Count > 0)
        {
            TempData["Error"] = $"Không thể xoá \"{entity.Name}\" vì có {entity.Children.Count} danh mục con. Hãy xoá hoặc chuyển danh mục con trước.";
            return RedirectToAction(nameof(Index));
        }

        if (entity.Products.Count > 0)
        {
            TempData["Error"] = $"Không thể xoá \"{entity.Name}\" vì có {entity.Products.Count} sản phẩm đang thuộc danh mục này.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã xoá danh mục \"{entity.Name}\" thành công.";
        return RedirectToAction(nameof(Index));
    }

    // ══════════════════════════════════════════════════════════════════════
    // TOGGLE ACTIVE (AJAX)
    // ══════════════════════════════════════════════════════════════════════

    [HttpPost]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsActive  = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { isActive = entity.IsActive });
    }

    // ══════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Tạo danh sách dropdown danh mục cha, thụt lề theo cấp.</summary>
    private async Task<List<CategorySelectItem>> BuildParentOptionsAsync(long? excludeId)
    {
        var all = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.ParentId == null ? 0 : 1)
            .ThenBy(c => c.ParentId)
            .ThenBy(c => c.Position)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var result = new List<CategorySelectItem>();
        BuildTree(all, null, 0, excludeId, result);
        return result;
    }

    private static void BuildTree(
        List<Category> all,
        long?          parentId,
        int            depth,
        long?          excludeId,
        List<CategorySelectItem> result)
    {
        foreach (var cat in all.Where(c => c.ParentId == parentId))
        {
            if (cat.Id == excludeId) continue;  // bỏ qua nhánh cần loại trừ

            var prefix = depth == 0 ? "" : new string('─', depth * 2) + " ";
            result.Add(new CategorySelectItem
            {
                Id    = cat.Id,
                Label = prefix + cat.Name,
                Depth = depth,
            });

            BuildTree(all, cat.Id, depth + 1, excludeId, result);
        }
    }

    /// <summary>Kiểm tra xem candidateId có phải là hậu duệ của ancestorId không.</summary>
    private async Task<bool> IsDescendantAsync(long ancestorId, long candidateId)
    {
        var allChildren = await _db.Categories
            .AsNoTracking()
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync();

        var visited = new HashSet<long>();
        var queue   = new Queue<long>();
        queue.Enqueue(ancestorId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;

            foreach (var child in allChildren.Where(c => c.ParentId == current))
            {
                if (child.Id == candidateId) return true;
                queue.Enqueue(child.Id);
            }
        }

        return false;
    }

    /// <summary>Tạo slug từ tên tiếng Việt.</summary>
    public static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var slug = name.ToLowerInvariant();

        // Bảng chuyển đổi ký tự tiếng Việt
        var map = new Dictionary<string, string>
        {
            {"à","a"},{"á","a"},{"ả","a"},{"ã","a"},{"ạ","a"},
            {"ă","a"},{"ắ","a"},{"ằ","a"},{"ẳ","a"},{"ẵ","a"},{"ặ","a"},
            {"â","a"},{"ấ","a"},{"ầ","a"},{"ẩ","a"},{"ẫ","a"},{"ậ","a"},
            {"đ","d"},
            {"è","e"},{"é","e"},{"ẻ","e"},{"ẽ","e"},{"ẹ","e"},
            {"ê","e"},{"ế","e"},{"ề","e"},{"ể","e"},{"ễ","e"},{"ệ","e"},
            {"ì","i"},{"í","i"},{"ỉ","i"},{"ĩ","i"},{"ị","i"},
            {"ò","o"},{"ó","o"},{"ỏ","o"},{"õ","o"},{"ọ","o"},
            {"ô","o"},{"ố","o"},{"ồ","o"},{"ổ","o"},{"ỗ","o"},{"ộ","o"},
            {"ơ","o"},{"ớ","o"},{"ờ","o"},{"ở","o"},{"ỡ","o"},{"ợ","o"},
            {"ù","u"},{"ú","u"},{"ủ","u"},{"ũ","u"},{"ụ","u"},
            {"ư","u"},{"ứ","u"},{"ừ","u"},{"ử","u"},{"ữ","u"},{"ự","u"},
            {"ỳ","y"},{"ý","y"},{"ỷ","y"},{"ỹ","y"},{"ỵ","y"},
        };

        foreach (var kv in map)
            slug = slug.Replace(kv.Key, kv.Value);

        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');

        return slug;
    }
}
