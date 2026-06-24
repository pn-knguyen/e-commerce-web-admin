using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Services.Shipping.Providers;

public interface IShippingProviderGateway
{
    ShippingProvider Provider { get; }
    bool IsConfigured { get; }

    Task<ShippingProviderAddressListResponse<ShippingProviderProvince>> GetProvincesAsync(
        CancellationToken ct = default);

    Task<ShippingProviderAddressListResponse<ShippingProviderDistrict>> GetDistrictsAsync(
        int provinceId,
        CancellationToken ct = default);

    Task<ShippingProviderAddressListResponse<ShippingProviderWard>> GetWardsAsync(
        int districtId,
        CancellationToken ct = default);

    Task<ShippingProviderQuoteResponse> CreateQuoteAsync(
        ShippingProviderQuoteRequest request,
        CancellationToken ct = default);

    Task<ShippingProviderCreateOrderResponse> CreateOrderAsync(
        ShippingProviderCreateOrderRequest request,
        CancellationToken ct = default);

    Task<ShippingProviderOrderDetailResponse> GetOrderDetailAsync(
        string orderCode,
        CancellationToken ct = default);

    Task<ShippingProviderCancelOrderResponse> CancelOrderAsync(
        string orderCode,
        CancellationToken ct = default);
}

public sealed record ShippingProviderPackage(
    string Name,
    string Description,
    int Quantity,
    int WeightGrams,
    int LengthCm,
    int WidthCm,
    int HeightCm,
    int InsuranceValue);

public sealed record ShippingProviderProvince(int Id, string Name, string? Code);

public sealed record ShippingProviderDistrict(
    int Id,
    int ProvinceId,
    string Name,
    int? SupportType,
    int? Status);

public sealed record ShippingProviderWard(
    string Code,
    int DistrictId,
    string Name,
    int? SupportType,
    int? Status);

public sealed record ShippingProviderAddressListResponse<T>(
    bool Succeeded,
    string? ErrorMessage,
    IReadOnlyList<T> Items,
    string? RawPayloadJson);

public sealed record ShippingProviderQuoteRequest(
    string ClientOrderCode,
    int? FromDistrictId,
    string? FromWardCode,
    int ToDistrictId,
    string ToWardCode,
    int? ServiceId,
    int ServiceTypeId,
    int CodAmount,
    string? Coupon,
    IReadOnlyCollection<ShippingProviderPackage> Packages);

public sealed record ShippingProviderCreateOrderRequest(
    string ClientOrderCode,
    int PaymentTypeId,
    string RequiredNote,
    string? Note,
    string FromName,
    string FromPhone,
    string FromAddress,
    string FromWardName,
    string FromDistrictName,
    string FromProvinceName,
    string ReturnPhone,
    string ReturnAddress,
    int? ReturnDistrictId,
    string? ReturnWardCode,
    string ToName,
    string ToPhone,
    string ToAddress,
    int ToDistrictId,
    string ToWardCode,
    int CodAmount,
    string Content,
    int? ServiceId,
    int ServiceTypeId,
    string? Coupon,
    IReadOnlyCollection<ShippingProviderPackage> Packages);

public sealed record ShippingProviderQuoteResponse(
    bool Succeeded,
    string? ErrorMessage,
    string? ProviderQuoteId,
    decimal? Fee,
    string Currency,
    DateTime? ExpectedDeliveryAt,
    string? RawPayloadJson);

public sealed record ShippingProviderCreateOrderResponse(
    bool Succeeded,
    string? ErrorMessage,
    string? OrderCode,
    string? ProviderStatus,
    string? TrackingUrl,
    decimal? Fee,
    string Currency,
    DateTime? ExpectedDeliveryAt,
    string? RawPayloadJson);

public sealed record ShippingProviderCancelOrderResponse(
    bool Succeeded,
    string? ErrorMessage,
    string? RawPayloadJson);

public sealed record ShippingProviderOrderDetailResponse(
    bool Succeeded,
    string? ErrorMessage,
    string? OrderCode,
    string? ProviderStatus,
    string? Message,
    decimal? Fee,
    string Currency,
    DateTime? ExpectedDeliveryAt,
    DateTime? UpdatedAt,
    string? RawPayloadJson);
