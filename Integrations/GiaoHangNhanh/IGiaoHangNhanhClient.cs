namespace e_commerce_web_admin.Integrations.GiaoHangNhanh;

public interface IGiaoHangNhanhClient
{
    bool IsConfigured { get; }
    Task<GiaoHangNhanhAddressListResponse<GiaoHangNhanhProvince>> GetProvincesAsync(
        CancellationToken ct = default);
    Task<GiaoHangNhanhAddressListResponse<GiaoHangNhanhDistrict>> GetDistrictsAsync(
        int provinceId,
        CancellationToken ct = default);
    Task<GiaoHangNhanhAddressListResponse<GiaoHangNhanhWard>> GetWardsAsync(
        int districtId,
        CancellationToken ct = default);
    Task<GiaoHangNhanhQuoteResponse> CreateQuoteAsync(
        GiaoHangNhanhQuoteRequest request,
        CancellationToken ct = default);
    Task<GiaoHangNhanhCreateOrderResponse> CreateOrderAsync(
        GiaoHangNhanhCreateOrderRequest request,
        CancellationToken ct = default);
    Task<GiaoHangNhanhOrderDetailResponse> GetOrderDetailAsync(
        string orderCode,
        CancellationToken ct = default);
    Task<GiaoHangNhanhCancelOrderResponse> CancelOrderAsync(
        string orderCode,
        CancellationToken ct = default);
}
