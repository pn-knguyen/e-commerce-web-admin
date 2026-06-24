using e_commerce_web_admin.Integrations.GiaoHangNhanh;
using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Services.Shipping.Providers;

public sealed class GiaoHangNhanhShippingProviderGateway : IShippingProviderGateway
{
    private readonly IGiaoHangNhanhClient _client;

    public GiaoHangNhanhShippingProviderGateway(IGiaoHangNhanhClient client)
        => _client = client;

    public ShippingProvider Provider => ShippingProvider.GiaoHangNhanh;
    public bool IsConfigured => _client.IsConfigured;

    public async Task<ShippingProviderAddressListResponse<ShippingProviderProvince>> GetProvincesAsync(
        CancellationToken ct = default)
    {
        var result = await _client.GetProvincesAsync(ct);
        return new ShippingProviderAddressListResponse<ShippingProviderProvince>(
            result.Succeeded,
            result.ErrorMessage,
            result.Items.Select(item => new ShippingProviderProvince(
                item.ProvinceId,
                item.ProvinceName,
                item.Code)).ToList(),
            result.RawPayloadJson);
    }

    public async Task<ShippingProviderAddressListResponse<ShippingProviderDistrict>> GetDistrictsAsync(
        int provinceId,
        CancellationToken ct = default)
    {
        var result = await _client.GetDistrictsAsync(provinceId, ct);
        return new ShippingProviderAddressListResponse<ShippingProviderDistrict>(
            result.Succeeded,
            result.ErrorMessage,
            result.Items.Select(item => new ShippingProviderDistrict(
                item.DistrictId,
                item.ProvinceId,
                item.DistrictName,
                item.SupportType,
                item.Status)).ToList(),
            result.RawPayloadJson);
    }

    public async Task<ShippingProviderAddressListResponse<ShippingProviderWard>> GetWardsAsync(
        int districtId,
        CancellationToken ct = default)
    {
        var result = await _client.GetWardsAsync(districtId, ct);
        return new ShippingProviderAddressListResponse<ShippingProviderWard>(
            result.Succeeded,
            result.ErrorMessage,
            result.Items.Select(item => new ShippingProviderWard(
                item.WardCode,
                item.DistrictId,
                item.WardName,
                item.SupportType,
                item.Status)).ToList(),
            result.RawPayloadJson);
    }

    public async Task<ShippingProviderQuoteResponse> CreateQuoteAsync(
        ShippingProviderQuoteRequest request,
        CancellationToken ct = default)
    {
        var result = await _client.CreateQuoteAsync(new GiaoHangNhanhQuoteRequest(
            request.ClientOrderCode,
            request.FromDistrictId,
            request.FromWardCode,
            request.ToDistrictId,
            request.ToWardCode,
            request.ServiceId,
            request.ServiceTypeId,
            request.CodAmount,
            request.Coupon,
            request.Packages.Select(ToGiaoHangNhanhPackage).ToList()),
            ct);

        return new ShippingProviderQuoteResponse(
            result.Succeeded,
            result.ErrorMessage,
            result.ProviderQuoteId,
            result.Fee,
            result.Currency,
            result.ExpectedDeliveryAt,
            result.RawPayloadJson);
    }

    public async Task<ShippingProviderCreateOrderResponse> CreateOrderAsync(
        ShippingProviderCreateOrderRequest request,
        CancellationToken ct = default)
    {
        var result = await _client.CreateOrderAsync(new GiaoHangNhanhCreateOrderRequest(
            request.ClientOrderCode,
            request.PaymentTypeId,
            request.RequiredNote,
            request.Note,
            request.FromName,
            request.FromPhone,
            request.FromAddress,
            request.FromWardName,
            request.FromDistrictName,
            request.FromProvinceName,
            request.ReturnPhone,
            request.ReturnAddress,
            request.ReturnDistrictId,
            request.ReturnWardCode,
            request.ToName,
            request.ToPhone,
            request.ToAddress,
            request.ToDistrictId,
            request.ToWardCode,
            request.CodAmount,
            request.Content,
            request.ServiceId,
            request.ServiceTypeId,
            request.Coupon,
            request.Packages.Select(ToGiaoHangNhanhPackage).ToList()),
            ct);

        return new ShippingProviderCreateOrderResponse(
            result.Succeeded,
            result.ErrorMessage,
            result.OrderCode,
            result.ProviderStatus,
            result.TrackingUrl,
            result.Fee,
            result.Currency,
            result.ExpectedDeliveryAt,
            result.RawPayloadJson);
    }

    public async Task<ShippingProviderOrderDetailResponse> GetOrderDetailAsync(
        string orderCode,
        CancellationToken ct = default)
    {
        var result = await _client.GetOrderDetailAsync(orderCode, ct);
        return new ShippingProviderOrderDetailResponse(
            result.Succeeded,
            result.ErrorMessage,
            result.OrderCode,
            result.ProviderStatus,
            result.Message,
            result.Fee,
            result.Currency,
            result.ExpectedDeliveryAt,
            result.UpdatedAt,
            result.RawPayloadJson);
    }

    public async Task<ShippingProviderCancelOrderResponse> CancelOrderAsync(
        string orderCode,
        CancellationToken ct = default)
    {
        var result = await _client.CancelOrderAsync(orderCode, ct);
        return new ShippingProviderCancelOrderResponse(
            result.Succeeded,
            result.ErrorMessage,
            result.RawPayloadJson);
    }

    private static GiaoHangNhanhPackage ToGiaoHangNhanhPackage(ShippingProviderPackage package) =>
        new(
            package.Name,
            package.Description,
            package.Quantity,
            package.WeightGrams,
            package.LengthCm,
            package.WidthCm,
            package.HeightCm,
            package.InsuranceValue);
}
