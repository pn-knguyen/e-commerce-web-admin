using e_commerce_web_admin.Data;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Categories;

public sealed class CategoryHierarchyService : ICategoryHierarchyService
{
    private readonly ApplicationDbContext _db;

    public CategoryHierarchyService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryHierarchyNode>> GetNodesAsync(
        CancellationToken ct = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .Select(category => new CategoryHierarchyNode(category.Id, category.ParentId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<long>> GetSelfAndDescendantIdsAsync(
        long categoryId,
        CancellationToken ct = default)
    {
        var categories = await GetNodesAsync(ct);
        if (!categories.Any(category => category.Id == categoryId))
        {
            return [];
        }

        var childrenByParentId = categories
            .Where(category => category.ParentId.HasValue)
            .ToLookup(category => category.ParentId!.Value);
        var result = new List<long>();
        var queue = new Queue<long>();
        var visited = new HashSet<long>();
        queue.Enqueue(categoryId);

        while (queue.TryDequeue(out var currentId))
        {
            if (!visited.Add(currentId))
            {
                continue;
            }

            result.Add(currentId);
            foreach (var child in childrenByParentId[currentId])
            {
                queue.Enqueue(child.Id);
            }
        }

        return result;
    }

    public IReadOnlyList<EffectiveCategoryAssignment<TAssignment>> ResolveEffectiveAssignments<TAssignment, TKey>(
        IReadOnlyCollection<CategoryHierarchyNode> categories,
        IEnumerable<TAssignment> assignments,
        Func<TAssignment, long> categoryIdSelector,
        Func<TAssignment, TKey> keySelector)
        where TKey : notnull
    {
        var parentMap = categories.ToDictionary(category => category.Id, category => category.ParentId);
        var assignmentsByCategory = assignments.ToLookup(categoryIdSelector);
        var result = new List<EffectiveCategoryAssignment<TAssignment>>();

        foreach (var category in categories.OrderBy(category => category.Id))
        {
            var usedKeys = new HashSet<TKey>();
            var visitedCategoryIds = new HashSet<long>();
            long? currentCategoryId = category.Id;

            while (currentCategoryId.HasValue && visitedCategoryIds.Add(currentCategoryId.Value))
            {
                foreach (var assignment in assignmentsByCategory[currentCategoryId.Value])
                {
                    if (usedKeys.Add(keySelector(assignment)))
                    {
                        result.Add(new EffectiveCategoryAssignment<TAssignment>(
                            category.Id,
                            assignment));
                    }
                }

                currentCategoryId = parentMap.TryGetValue(currentCategoryId.Value, out var parentId)
                    ? parentId
                    : null;
            }
        }

        return result;
    }
}
