namespace e_commerce_web_admin.Integrations.GiaoHangNhanh;

public sealed class GiaoHangNhanhOptions
{
    public const string SectionName = "GiaoHangNhanh";

    public bool Enabled { get; set; }
    public bool UseMockResponses { get; set; }
    public string ApiBaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn/shiip/public-api";
    public string FeePath { get; set; } = "/v2/shipping-order/fee";
    public string CreateOrderPath { get; set; } = "/v2/shipping-order/create";
    public string OrderDetailPath { get; set; } = "/v2/shipping-order/detail";
    public string CancelOrderPath { get; set; } = "/v2/switch-status/cancel";
    public string ProvincePath { get; set; } = "/master-data/province";
    public string DistrictPath { get; set; } = "/master-data/district";
    public string WardPath { get; set; } = "/master-data/ward";
    public string? Token { get; set; }
    public int? ShopId { get; set; }
    public int? ServiceId { get; set; }
    public int ServiceTypeId { get; set; } = 2;
    public int PaymentTypeId { get; set; } = 1;
    public string RequiredNote { get; set; } = "KHONGCHOXEMHANG";
    public string? Coupon { get; set; }
    public int DefaultWeightGrams { get; set; } = 500;
    public int DefaultLengthCm { get; set; } = 10;
    public int DefaultWidthCm { get; set; } = 10;
    public int DefaultHeightCm { get; set; } = 10;
    public int MaxInsuranceValue { get; set; } = 5_000_000;
    public bool EnableCodForUnpaidOrders { get; set; }
    public string TrackingUrlTemplate { get; set; } = "https://tracking.ghn.dev/?order_code={orderCode}";
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableBackgroundStatusSync { get; set; } = true;
    public int StatusSyncIntervalSeconds { get; set; } = 180;
    public int StatusSyncBatchSize { get; set; } = 20;
    public int BookingRecoveryMinutes { get; set; } = 5;
    public bool EnableWebhookProcessing { get; set; }
    public string? WebhookSecret { get; set; }
    public bool EnableWebhookSignatureValidation { get; set; } = true;
}
