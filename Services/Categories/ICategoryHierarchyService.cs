namespace e_commerce_web_admin.Services.Categories;

public interface ICategoryHierarchyService
{
    Task<IReadOnlyList<CategoryHierarchyNode>> GetNodesAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<long>> GetSelfAndDescendantIdsAsync(
        long categoryId,
        CancellationToken ct = default);

    IReadOnlyList<EffectiveCategoryAssignment<TAssignment>> ResolveEffectiveAssignments<TAssignment, TKey>(
        IReadOnlyCollection<CategoryHierarchyNode> categories,
        IEnumerable<TAssignment> assignments,
        Func<TAssignment, long> categoryIdSelector,
        Func<TAssignment, TKey> keySelector)
        where TKey : notnull;
}

public sealed record CategoryHierarchyNode(long Id, long? ParentId);

public sealed record EffectiveCategoryAssignment<TAssignment>(
    long CategoryId,
    TAssignment Assignment);
