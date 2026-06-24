using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.FulfillmentLocations;

public sealed class FulfillmentLocationIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class FulfillmentLocationIndexViewModel
{
    public List<FulfillmentLocationRowViewModel> Locations { get; set; } = [];
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int DefaultCount { get; set; }
    public int ShipmentCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Status);
}

public sealed class FulfillmentLocationRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ProvinceName { get; set; } = string.Empty;
    public string? DistrictCode { get; set; }
    public string? DistrictName { get; set; }
    public string WardName { get; set; } = string.Empty;
    public string DetailAddress { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int ShipmentCount { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    public bool IsDeleteBlocked => ShipmentCount > 0;
}

public sealed class FulfillmentLocationFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên điểm lấy hàng là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên điểm lấy hàng tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên người phụ trách là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên người phụ trách tối đa 255 ký tự.")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã tỉnh/thành GHN là bắt buộc.")]
    [StringLength(30, ErrorMessage = "Mã tỉnh/thành tối đa 30 ký tự.")]
    public string? ProvinceCode { get; set; }

    [Required(ErrorMessage = "Tỉnh/thành là bắt buộc.")]
    [StringLength(120, ErrorMessage = "Tỉnh/thành tối đa 120 ký tự.")]
    public string ProvinceName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã quận/huyện GHN là bắt buộc.")]
    [StringLength(30, ErrorMessage = "Mã quận/huyện GHN tối đa 30 ký tự.")]
    public string? DistrictCode { get; set; }

    [Required(ErrorMessage = "Quận/huyện là bắt buộc.")]
    [StringLength(120, ErrorMessage = "Quận/huyện tối đa 120 ký tự.")]
    public string? DistrictName { get; set; }

    [Required(ErrorMessage = "Mã phường/xã GHN là bắt buộc.")]
    [StringLength(30, ErrorMessage = "Mã phường/xã tối đa 30 ký tự.")]
    public string? WardCode { get; set; }

    [Required(ErrorMessage = "Phường/xã là bắt buộc.")]
    [StringLength(120, ErrorMessage = "Phường/xã tối đa 120 ký tự.")]
    public string WardName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc.")]
    [StringLength(500, ErrorMessage = "Địa chỉ chi tiết tối đa 500 ký tự.")]
    public string DetailAddress { get; set; } = string.Empty;

    [StringLength(700, ErrorMessage = "Địa chỉ chuẩn hóa tối đa 700 ký tự.")]
    public string? FormattedAddress { get; set; }

    [Range(typeof(decimal), "-90", "90", ErrorMessage = "Vĩ độ phải nằm trong khoảng -90 đến 90.")]
    public decimal? Latitude { get; set; }

    [Range(typeof(decimal), "-180", "180", ErrorMessage = "Kinh độ phải nằm trong khoảng -180 đến 180.")]
    public decimal? Longitude { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsEdit => Id > 0;
}

public sealed record FulfillmentLocationValidationError(string FieldName, string Message);

public sealed class FulfillmentLocationSaveResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public FulfillmentLocationFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<FulfillmentLocationValidationError> Errors { get; init; } = [];

    public static FulfillmentLocationSaveResult Success(FulfillmentLocationFormViewModel form, string message) =>
        new() { Succeeded = true, Form = form, Message = message };

    public static FulfillmentLocationSaveResult Failed(
        FulfillmentLocationFormViewModel form,
        IReadOnlyCollection<FulfillmentLocationValidationError> errors) =>
        new() { Succeeded = false, Form = form, Errors = errors };
}

public sealed class FulfillmentLocationActionResult
{
    public bool Found { get; init; } = true;
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static FulfillmentLocationActionResult NotFound() =>
        new() { Found = false, Succeeded = false };

    public static FulfillmentLocationActionResult Success(string message) =>
        new() { Found = true, Succeeded = true, Message = message };

    public static FulfillmentLocationActionResult Failed(string message) =>
        new() { Found = true, Succeeded = false, Message = message };
}

public sealed record FulfillmentLocationToggleResult(bool IsActive, bool IsDefault);
