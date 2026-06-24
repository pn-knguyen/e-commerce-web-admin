using System.Text.Json;

namespace e_commerce_web_admin.Integrations.GiaoHangNhanh;

public sealed record GiaoHangNhanhPackage(
    string Name,
    string Description,
    int Quantity,
    int WeightGrams,
    int LengthCm,
    int WidthCm,
    int HeightCm,
    int InsuranceValue);

public sealed record GiaoHangNhanhProvince(
    int ProvinceId,
    string ProvinceName,
    string? Code);

public sealed record GiaoHangNhanhDistrict(
    int DistrictId,
    int ProvinceId,
    string DistrictName,
    int? SupportType,
    int? Status);

public sealed record GiaoHangNhanhWard(
    string WardCode,
    int DistrictId,
    string WardName,
    int? SupportType,
    int? Status);

public sealed record GiaoHangNhanhAddressListResponse<T>(
    bool Succeeded,
    string? ErrorMessage,
    IReadOnlyList<T> Items,
    string? RawPayloadJson)
{
    public static GiaoHangNhanhAddressListResponse<T> Success(
        IReadOnlyList<T> items,
        string? rawPayload = null) =>
        new(true, null, items, rawPayload);

    public static GiaoHangNhanhAddressListResponse<T> Failed(
        string message,
        string? rawPayload = null) =>
        new(false, message, [], rawPayload);
}

public sealed record GiaoHangNhanhQuoteRequest(
    string ClientOrderCode,
    int? FromDistrictId,
    string? FromWardCode,
    int ToDistrictId,
    string ToWardCode,
    int? ServiceId,
    int ServiceTypeId,
    int CodAmount,
    string? Coupon,
    IReadOnlyCollection<GiaoHangNhanhPackage> Packages);

public sealed record GiaoHangNhanhCreateOrderRequest(
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
    IReadOnlyCollection<GiaoHangNhanhPackage> Packages);

public sealed record GiaoHangNhanhQuoteResponse(
    bool Succeeded,
    string? ErrorMessage,
    string? ProviderQuoteId,
    decimal? Fee,
    string Currency,
    DateTime? ExpectedDeliveryAt,
    string? RawPayloadJson)
{
    public static GiaoHangNhanhQuoteResponse Failed(string message, string? rawPayload = null) =>
        new(false, message, null, null, "VND", null, rawPayload);
}

public sealed record GiaoHangNhanhCreateOrderResponse(
    bool Succeeded,
    string? ErrorMessage,
    string? OrderCode,
    string? ProviderStatus,
    string? TrackingUrl,
    decimal? Fee,
    string Currency,
    DateTime? ExpectedDeliveryAt,
    string? RawPayloadJson)
{
    public static GiaoHangNhanhCreateOrderResponse Failed(string message, string? rawPayload = null) =>
        new(false, message, null, null, null, null, "VND", null, rawPayload);
}

public sealed record GiaoHangNhanhCancelOrderResponse(
    bool Succeeded,
    string? ErrorMessage,
    string? RawPayloadJson)
{
    public static GiaoHangNhanhCancelOrderResponse Failed(string message, string? rawPayload = null) =>
        new(false, message, rawPayload);
}

public sealed record GiaoHangNhanhOrderDetailResponse(
    bool Succeeded,
    string? ErrorMessage,
    string? OrderCode,
    string? ProviderStatus,
    string? Message,
    decimal? Fee,
    string Currency,
    DateTime? ExpectedDeliveryAt,
    DateTime? UpdatedAt,
    string? RawPayloadJson)
{
    public static GiaoHangNhanhOrderDetailResponse Failed(string message, string? rawPayload = null) =>
        new(false, message, null, null, null, null, "VND", null, null, rawPayload);
}

public sealed record GiaoHangNhanhWebhookPayload(
    string? OrderCode,
    string? Status,
    string? Description,
    DateTime? UpdatedAt,
    JsonDocument RawPayload);
