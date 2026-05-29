using e_commerce_web_admin.ViewModels.Attributes;

namespace e_commerce_web_admin.Services.Attributes;

public interface IAttributeAdminService
{
    // ── Attribute CRUD ─────────────────────────────────────────────────────
    Task<AttributeIndexViewModel> GetIndexAsync(AttributeIndexQuery query, CancellationToken ct = default);
    Task<AttributeFormViewModel> GetCreateFormAsync(CancellationToken ct = default);
    Task<AttrSaveResult> CreateAsync(AttributeFormViewModel form, CancellationToken ct = default);
    Task<AttributeEditViewModel?> GetEditViewAsync(long id, CancellationToken ct = default);
    Task<AttrSaveResult> UpdateAsync(long id, AttributeFormViewModel form, CancellationToken ct = default);
    Task<AttrDeleteResult> DeleteAsync(long id, CancellationToken ct = default);

    // ── AttributeOption CRUD (AJAX) ────────────────────────────────────────
    Task<AttributeOptionsViewModel?> GetOptionsAsync(long attributeId, CancellationToken ct = default);
    Task<AttrOptionSaveResult> AddOptionAsync(AttributeOptionFormViewModel form, CancellationToken ct = default);
    Task<AttrOptionSaveResult> UpdateOptionAsync(AttributeOptionUpdateViewModel form, CancellationToken ct = default);
    Task<AttrOptionDeleteResult> DeleteOptionAsync(long optionId, CancellationToken ct = default);
}
