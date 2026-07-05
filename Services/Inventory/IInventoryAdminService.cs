using e_commerce_web_admin.ViewModels.Inventory;

namespace e_commerce_web_admin.Services.Inventory;

public interface IInventoryAdminService
{
    Task<InventoryIndexViewModel> GetIndexAsync(
        InventoryIndexQuery query,
        CancellationToken ct = default);

    Task<GoodsReceiptDetailsViewModel?> GetDetailsAsync(
        long id,
        CancellationToken ct = default);

    Task<InventoryStockDetailsViewModel?> GetStockDetailsAsync(
        long variantId,
        CancellationToken ct = default);

    Task<GoodsReceiptFormViewModel> GetCreateFormAsync(
        long? variantId = null,
        CancellationToken ct = default);

    Task<GoodsReceiptFormViewModel?> GetEditFormAsync(
        long id,
        CancellationToken ct = default);

    Task<GoodsReceiptFormViewModel> PrepareFormAsync(
        GoodsReceiptFormViewModel form,
        CancellationToken ct = default);

    Task<GoodsReceiptSaveResult> CreateAsync(
        GoodsReceiptFormViewModel form,
        CancellationToken ct = default);

    Task<GoodsReceiptSaveResult> UpdateAsync(
        long id,
        GoodsReceiptFormViewModel form,
        CancellationToken ct = default);

    Task<GoodsReceiptActionResult> SubmitAsync(
        long id,
        CancellationToken ct = default);

    Task<GoodsReceiptActionResult> ApproveAsync(
        long id,
        CancellationToken ct = default);

    Task<GoodsReceiptActionResult> CancelAsync(
        long id,
        CancellationToken ct = default);

    Task<GoodsReceiptActionResult> DeleteAsync(
        long id,
        CancellationToken ct = default);
}
