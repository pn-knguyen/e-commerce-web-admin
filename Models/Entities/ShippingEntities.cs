using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Models.Entities;

public class FulfillmentLocation
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? ProvinceCode { get; set; }
    public string ProvinceName { get; set; } = string.Empty;
    public string? DistrictCode { get; set; }
    public string? DistrictName { get; set; }
    public string? WardCode { get; set; }
    public string WardName { get; set; } = string.Empty;
    public string DetailAddress { get; set; } = string.Empty;
    public string? FormattedAddress { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}

public class Shipment
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long? FulfillmentLocationId { get; set; }
    public ShippingProvider Provider { get; set; } = ShippingProvider.GiaoHangNhanh;
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Draft;
    public string? ProviderDeliveryId { get; set; }
    public string? ProviderQuoteId { get; set; }
    public string? ProviderStatus { get; set; }
    public string? TrackingUrl { get; set; }

    public string PickupContactName { get; set; } = string.Empty;
    public string PickupPhone { get; set; } = string.Empty;
    public string? PickupDetailAddress { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public decimal? PickupLatitude { get; set; }
    public decimal? PickupLongitude { get; set; }
    public string? ProviderPickupProvinceCode { get; set; }
    public string? ProviderPickupProvinceName { get; set; }
    public string? ProviderPickupDistrictCode { get; set; }
    public string? ProviderPickupDistrictName { get; set; }
    public string? ProviderPickupWardCode { get; set; }
    public string? ProviderPickupWardName { get; set; }

    public string DropoffContactName { get; set; } = string.Empty;
    public string DropoffPhone { get; set; } = string.Empty;
    public string? DropoffDetailAddress { get; set; }
    public string DropoffAddress { get; set; } = string.Empty;
    public decimal? DropoffLatitude { get; set; }
    public decimal? DropoffLongitude { get; set; }
    public string? ProviderDropoffProvinceCode { get; set; }
    public string? ProviderDropoffProvinceName { get; set; }
    public string? ProviderDropoffDistrictCode { get; set; }
    public string? ProviderDropoffDistrictName { get; set; }
    public string? ProviderDropoffWardCode { get; set; }
    public string? ProviderDropoffWardName { get; set; }

    public decimal? QuotedFee { get; set; }
    public decimal? ActualFee { get; set; }
    public string Currency { get; set; } = "VND";
    public int? EstimatedDistanceMeters { get; set; }
    public int? EstimatedDurationSeconds { get; set; }
    public long? RequestedByStaffId { get; set; }
    public DateTime? BookedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Order? Order { get; set; }
    public FulfillmentLocation? FulfillmentLocation { get; set; }
    public Staff? RequestedByStaff { get; set; }
    public ICollection<ShipmentPackage> Packages { get; set; } = new List<ShipmentPackage>();
    public ICollection<ShipmentEvent> Events { get; set; } = new List<ShipmentEvent>();
}

public class ShipmentPackage
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public int Sequence { get; set; } = 1;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public int? WeightGrams { get; set; }
    public decimal? LengthCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DeclaredValue { get; set; }
    public bool IsFragile { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Shipment? Shipment { get; set; }
}

public class ShipmentEvent
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public string? ProviderEventId { get; set; }
    public string? ProviderStatus { get; set; }
    public ShipmentStatus Status { get; set; }
    public string? Message { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? VehiclePlate { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? RawPayloadJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Shipment? Shipment { get; set; }
}
