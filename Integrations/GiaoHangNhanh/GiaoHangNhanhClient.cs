using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace e_commerce_web_admin.Integrations.GiaoHangNhanh;

public sealed class GiaoHangNhanhClient : IGiaoHangNhanhClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly GiaoHangNhanhOptions _options;

    public GiaoHangNhanhClient(HttpClient httpClient, IOptions<GiaoHangNhanhOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds));
    }

    public bool IsConfigured =>
        _options.Enabled &&
        (_options.UseMockResponses ||
            (!string.IsNullOrWhiteSpace(_options.Token) && _options.ShopId.HasValue));

    private bool HasAddressLookupConfigured =>
        _options.Enabled &&
        (_options.UseMockResponses || !string.IsNullOrWhiteSpace(_options.Token));

    public async Task<GiaoHangNhanhAddressListResponse<GiaoHangNhanhProvince>> GetProvincesAsync(
        CancellationToken ct = default)
    {
        if (!HasAddressLookupConfigured)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhProvince>.Failed(
                "GHN chưa được cấu hình Token để lấy danh sách tỉnh/thành.");
        }

        if (_options.UseMockResponses)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhProvince>.Success(
            [
                new(202, "Hồ Chí Minh", "8"),
                new(201, "Hà Nội", "4"),
            ]);
        }

        var response = await SendAsync(HttpMethod.Get, _options.ProvincePath, body: null, ct, includeShopId: false);
        if (!response.Succeeded)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhProvince>.Failed(
                response.ErrorMessage ?? "GHN không trả về danh sách tỉnh/thành.",
                response.RawPayload);
        }

        return ParseAddressList(response.RawPayload, ParseProvince, "GHN không trả về danh sách tỉnh/thành.");
    }

    public async Task<GiaoHangNhanhAddressListResponse<GiaoHangNhanhDistrict>> GetDistrictsAsync(
        int provinceId,
        CancellationToken ct = default)
    {
        if (!HasAddressLookupConfigured)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhDistrict>.Failed(
                "GHN chưa được cấu hình Token để lấy danh sách quận/huyện.");
        }

        if (provinceId <= 0)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhDistrict>.Failed(
                "Mã tỉnh/thành GHN không hợp lệ.");
        }

        if (_options.UseMockResponses)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhDistrict>.Success(
            [
                new(1442, 202, "Quận 1", 3, 1),
                new(3695, 202, "Thành Phố Thủ Đức", 3, 1),
            ]);
        }

        var response = await SendAsync(
            HttpMethod.Post,
            _options.DistrictPath,
            new { province_id = provinceId },
            ct,
            includeShopId: false);
        if (!response.Succeeded)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhDistrict>.Failed(
                response.ErrorMessage ?? "GHN không trả về danh sách quận/huyện.",
                response.RawPayload);
        }

        return ParseAddressList(response.RawPayload, ParseDistrict, "GHN không trả về danh sách quận/huyện.");
    }

    public async Task<GiaoHangNhanhAddressListResponse<GiaoHangNhanhWard>> GetWardsAsync(
        int districtId,
        CancellationToken ct = default)
    {
        if (!HasAddressLookupConfigured)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhWard>.Failed(
                "GHN chưa được cấu hình Token để lấy danh sách phường/xã.");
        }

        if (districtId <= 0)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhWard>.Failed(
                "Mã quận/huyện GHN không hợp lệ.");
        }

        if (_options.UseMockResponses)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhWard>.Success(
            [
                new("20308", 1442, "Phường Bến Nghé", 3, 1),
                new("90768", 3695, "Phường An Khánh", 3, 1),
            ]);
        }

        var response = await SendAsync(
            HttpMethod.Post,
            _options.WardPath,
            new { district_id = districtId },
            ct,
            includeShopId: false);
        if (!response.Succeeded)
        {
            return GiaoHangNhanhAddressListResponse<GiaoHangNhanhWard>.Failed(
                response.ErrorMessage ?? "GHN không trả về danh sách phường/xã.",
                response.RawPayload);
        }

        return ParseAddressList(response.RawPayload, ParseWard, "GHN không trả về danh sách phường/xã.");
    }

    public async Task<GiaoHangNhanhQuoteResponse> CreateQuoteAsync(
        GiaoHangNhanhQuoteRequest request,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return GiaoHangNhanhQuoteResponse.Failed("GHN chưa được cấu hình Token hoặc ShopId.");
        }

        if (_options.UseMockResponses)
        {
            return new GiaoHangNhanhQuoteResponse(
                true,
                null,
                $"GHN-MOCK-{request.ClientOrderCode}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                36300m,
                "VND",
                DateTime.UtcNow.AddDays(2),
                "{\"mock\":true}");
        }

        var package = request.Packages.FirstOrDefault();
        if (package is null)
        {
            return GiaoHangNhanhQuoteResponse.Failed("Vui lòng nhập ít nhất một kiện hàng để GHN tính phí.");
        }

        var body = new
        {
            from_district_id = request.FromDistrictId,
            from_ward_code = request.FromWardCode,
            service_id = request.ServiceId,
            service_type_id = request.ServiceId.HasValue ? null : (int?)request.ServiceTypeId,
            to_district_id = request.ToDistrictId,
            to_ward_code = request.ToWardCode,
            height = package.HeightCm,
            length = package.LengthCm,
            weight = package.WeightGrams,
            width = package.WidthCm,
            insurance_value = package.InsuranceValue,
            cod_value = request.CodAmount,
            coupon = request.Coupon,
            items = request.Packages.Select(BuildItem).ToArray(),
        };

        var response = await SendAsync(_options.FeePath, body, ct);
        if (!response.Succeeded)
        {
            return GiaoHangNhanhQuoteResponse.Failed(
                response.ErrorMessage ?? "GHN không trả về báo giá.",
                response.RawPayload);
        }

        return ParseQuoteResponse(response.RawPayload);
    }

    public async Task<GiaoHangNhanhCreateOrderResponse> CreateOrderAsync(
        GiaoHangNhanhCreateOrderRequest request,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return GiaoHangNhanhCreateOrderResponse.Failed("GHN chưa được cấu hình Token hoặc ShopId.");
        }

        if (_options.UseMockResponses)
        {
            var orderCode = $"GHNMOCK{DateTime.UtcNow:HHmmss}";
            return new GiaoHangNhanhCreateOrderResponse(
                true,
                null,
                orderCode,
                "ready_to_pick",
                BuildTrackingUrl(orderCode),
                36300m,
                "VND",
                DateTime.UtcNow.AddDays(2),
                "{\"mock\":true}");
        }

        var package = request.Packages.FirstOrDefault();
        if (package is null)
        {
            return GiaoHangNhanhCreateOrderResponse.Failed("Vui lòng nhập ít nhất một kiện hàng để GHN tạo vận đơn.");
        }

        var body = new
        {
            payment_type_id = request.PaymentTypeId,
            note = request.Note,
            required_note = request.RequiredNote,
            from_name = request.FromName,
            from_phone = request.FromPhone,
            from_address = request.FromAddress,
            from_ward_name = request.FromWardName,
            from_district_name = request.FromDistrictName,
            from_province_name = request.FromProvinceName,
            return_phone = request.ReturnPhone,
            return_address = request.ReturnAddress,
            return_district_id = request.ReturnDistrictId,
            return_ward_code = request.ReturnWardCode,
            client_order_code = request.ClientOrderCode,
            to_name = request.ToName,
            to_phone = request.ToPhone,
            to_address = request.ToAddress,
            to_ward_code = request.ToWardCode,
            to_district_id = request.ToDistrictId,
            cod_amount = request.CodAmount,
            content = request.Content,
            weight = package.WeightGrams,
            length = package.LengthCm,
            width = package.WidthCm,
            height = package.HeightCm,
            insurance_value = package.InsuranceValue,
            service_id = request.ServiceId,
            service_type_id = request.ServiceId.HasValue ? null : (int?)request.ServiceTypeId,
            coupon = request.Coupon,
            items = request.Packages.Select(BuildItem).ToArray(),
        };

        var response = await SendAsync(_options.CreateOrderPath, body, ct);
        if (!response.Succeeded)
        {
            return GiaoHangNhanhCreateOrderResponse.Failed(
                response.ErrorMessage ?? "GHN không tạo được vận đơn.",
                response.RawPayload);
        }

        return ParseCreateOrderResponse(response.RawPayload);
    }

    public async Task<GiaoHangNhanhOrderDetailResponse> GetOrderDetailAsync(
        string orderCode,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return GiaoHangNhanhOrderDetailResponse.Failed("GHN chua duoc cau hinh Token hoac ShopId.");
        }

        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return GiaoHangNhanhOrderDetailResponse.Failed("Ma van don GHN khong hop le.");
        }

        if (_options.UseMockResponses)
        {
            return new GiaoHangNhanhOrderDetailResponse(
                true,
                null,
                orderCode.Trim(),
                "ready_to_pick",
                "Mock GHN order detail",
                null,
                "VND",
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow,
                "{\"mock\":true}");
        }

        var response = await SendAsync(
            HttpMethod.Post,
            _options.OrderDetailPath,
            new { order_code = orderCode.Trim() },
            ct,
            includeShopId: false);
        if (!response.Succeeded)
        {
            return GiaoHangNhanhOrderDetailResponse.Failed(
                response.ErrorMessage ?? "GHN khong tra ve chi tiet van don.",
                response.RawPayload);
        }

        return ParseOrderDetailResponse(response.RawPayload, orderCode.Trim());
    }

    public async Task<GiaoHangNhanhCancelOrderResponse> CancelOrderAsync(
        string orderCode,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return GiaoHangNhanhCancelOrderResponse.Failed("GHN chưa được cấu hình Token hoặc ShopId.");
        }

        if (_options.UseMockResponses)
        {
            return new GiaoHangNhanhCancelOrderResponse(true, null, "{\"mock\":true}");
        }

        var body = new { order_codes = new[] { orderCode } };
        var response = await SendAsync(_options.CancelOrderPath, body, ct);
        if (!response.Succeeded)
        {
            return GiaoHangNhanhCancelOrderResponse.Failed(
                response.ErrorMessage ?? "GHN không hủy được vận đơn.",
                response.RawPayload);
        }

        return ParseCancelOrderResponse(response.RawPayload);
    }

    private Task<(bool Succeeded, string? ErrorMessage, string? RawPayload)> SendAsync(
        string path,
        object body,
        CancellationToken ct) =>
        SendAsync(HttpMethod.Post, path, body, ct, includeShopId: true);

    private async Task<(bool Succeeded, string? ErrorMessage, string? RawPayload)> SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct,
        bool includeShopId)
    {
        using var request = new HttpRequestMessage(method, BuildApiUri(path));
        request.Headers.TryAddWithoutValidation("Token", _options.Token);
        if (includeShopId)
        {
            request.Headers.TryAddWithoutValidation("ShopId", _options.ShopId?.ToString(CultureInfo.InvariantCulture));
        }

        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await _httpClient.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                return (false, BuildHttpError(response.StatusCode, raw), raw);
            }

            var apiError = ReadGhnApiError(raw);
            return string.IsNullOrWhiteSpace(apiError)
                ? (true, null, raw)
                : (false, apiError, raw);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return (false, "GHN phản hồi quá thời gian cho phép.", null);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Không kết nối được GHN: {ex.Message}", null);
        }
    }

    private GiaoHangNhanhQuoteResponse ParseQuoteResponse(string? rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return GiaoHangNhanhQuoteResponse.Failed("GHN không trả về nội dung báo giá.");
        }

        try
        {
            using var json = JsonDocument.Parse(rawPayload);
            var data = TryGetProperty(json.RootElement, "data", out var dataElement) ? dataElement : json.RootElement;
            var fee = ReadDecimal(data, "total") ?? ReadDecimal(data, "service_fee");

            return new GiaoHangNhanhQuoteResponse(
                true,
                null,
                null,
                fee,
                "VND",
                null,
                rawPayload);
        }
        catch (JsonException)
        {
            return GiaoHangNhanhQuoteResponse.Failed("GHN trả về nội dung báo giá không hợp lệ.", rawPayload);
        }
    }

    private GiaoHangNhanhCreateOrderResponse ParseCreateOrderResponse(string? rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return GiaoHangNhanhCreateOrderResponse.Failed("GHN không trả về nội dung tạo vận đơn.");
        }

        try
        {
            using var json = JsonDocument.Parse(rawPayload);
            var data = TryGetProperty(json.RootElement, "data", out var dataElement) ? dataElement : json.RootElement;
            var orderCode = ReadString(data, "order_code");
            var fee = ReadDecimal(data, "total_fee") ?? ReadDecimal(data, "fee", "main_service");
            var expectedDeliveryAt = ReadDateTime(data, "expected_delivery_time");
            var providerStatus = ReadString(data, "status") ?? "ready_to_pick";

            return new GiaoHangNhanhCreateOrderResponse(
                true,
                null,
                orderCode,
                providerStatus,
                string.IsNullOrWhiteSpace(orderCode) ? null : BuildTrackingUrl(orderCode),
                fee,
                "VND",
                expectedDeliveryAt,
                rawPayload);
        }
        catch (JsonException)
        {
            return GiaoHangNhanhCreateOrderResponse.Failed("GHN trả về nội dung tạo vận đơn không hợp lệ.", rawPayload);
        }
    }

    private static GiaoHangNhanhCancelOrderResponse ParseCancelOrderResponse(string? rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return GiaoHangNhanhCancelOrderResponse.Failed("GHN không trả về nội dung hủy vận đơn.");
        }

        try
        {
            using var json = JsonDocument.Parse(rawPayload);
            if (TryGetProperty(json.RootElement, "data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (TryGetProperty(item, "result", out var result) &&
                        result.ValueKind == JsonValueKind.False)
                    {
                        return GiaoHangNhanhCancelOrderResponse.Failed(
                            ReadString(item, "message") ?? "GHN từ chối hủy vận đơn.",
                            rawPayload);
                    }
                }
            }

            return new GiaoHangNhanhCancelOrderResponse(true, null, rawPayload);
        }
        catch (JsonException)
        {
            return GiaoHangNhanhCancelOrderResponse.Failed("GHN trả về nội dung hủy vận đơn không hợp lệ.", rawPayload);
        }
    }

    private static GiaoHangNhanhOrderDetailResponse ParseOrderDetailResponse(string? rawPayload, string requestedOrderCode)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return GiaoHangNhanhOrderDetailResponse.Failed("GHN khong tra ve noi dung chi tiet van don.");
        }

        try
        {
            using var json = JsonDocument.Parse(rawPayload);
            var data = TryGetProperty(json.RootElement, "data", out var dataElement)
                ? dataElement
                : json.RootElement;

            if (data.ValueKind == JsonValueKind.Array)
            {
                data = data.EnumerateArray().FirstOrDefault();
            }

            if (data.ValueKind != JsonValueKind.Object)
            {
                return GiaoHangNhanhOrderDetailResponse.Failed("GHN khong tra ve chi tiet van don hop le.", rawPayload);
            }

            var latestLog = GetLatestLog(data);
            var status = ReadString(data, "status") ?? ReadString(latestLog, "status");
            var updatedAt = ReadDateTime(data, "updated_date") ??
                ReadDateTime(latestLog, "updated_date") ??
                ReadDateTime(data, "finish_date") ??
                ReadDateTime(data, "pickup_time") ??
                ReadDateTime(data, "order_date");

            return new GiaoHangNhanhOrderDetailResponse(
                true,
                null,
                ReadString(data, "order_code") ?? requestedOrderCode,
                status,
                ReadString(data, "reason") ?? ReadString(data, "content") ?? ReadString(data, "note"),
                ReadDecimal(data, "total_fee") ?? ReadDecimal(data, "fee", "total"),
                "VND",
                ReadDateTime(data, "leadtime") ?? ReadDateTime(data, "expected_delivery_time"),
                updatedAt,
                rawPayload);
        }
        catch (JsonException)
        {
            return GiaoHangNhanhOrderDetailResponse.Failed("GHN khong tra ve noi dung chi tiet van don hop le.", rawPayload);
        }
    }

    private static GiaoHangNhanhAddressListResponse<T> ParseAddressList<T>(
        string? rawPayload,
        Func<JsonElement, T?> parseItem,
        string emptyMessage)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return GiaoHangNhanhAddressListResponse<T>.Failed(emptyMessage);
        }

        try
        {
            using var json = JsonDocument.Parse(rawPayload);
            var data = TryGetProperty(json.RootElement, "data", out var dataElement)
                ? dataElement
                : json.RootElement;
            var items = new List<T>();

            if (data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    var parsed = parseItem(item);
                    if (parsed is not null)
                    {
                        items.Add(parsed);
                    }
                }
            }
            else if (data.ValueKind == JsonValueKind.Object)
            {
                var parsed = parseItem(data);
                if (parsed is not null)
                {
                    items.Add(parsed);
                }
            }

            return items.Count == 0
                ? GiaoHangNhanhAddressListResponse<T>.Failed(emptyMessage, rawPayload)
                : GiaoHangNhanhAddressListResponse<T>.Success(items, rawPayload);
        }
        catch (JsonException)
        {
            return GiaoHangNhanhAddressListResponse<T>.Failed(emptyMessage, rawPayload);
        }
    }

    private static GiaoHangNhanhProvince? ParseProvince(JsonElement item)
    {
        var id = ReadAnyInt(item, "ProvinceID", "province_id");
        var name = ReadAnyString(item, "ProvinceName", "province_name");
        return id.HasValue && !string.IsNullOrWhiteSpace(name)
            ? new GiaoHangNhanhProvince(id.Value, name, ReadAnyString(item, "Code", "code"))
            : null;
    }

    private static GiaoHangNhanhDistrict? ParseDistrict(JsonElement item)
    {
        var id = ReadAnyInt(item, "DistrictID", "district_id");
        var provinceId = ReadAnyInt(item, "ProvinceID", "province_id");
        var name = ReadAnyString(item, "DistrictName", "district_name");
        return id.HasValue && provinceId.HasValue && !string.IsNullOrWhiteSpace(name)
            ? new GiaoHangNhanhDistrict(
                id.Value,
                provinceId.Value,
                name,
                ReadAnyInt(item, "SupportType", "support_type"),
                ReadAnyInt(item, "Status", "status"))
            : null;
    }

    private static GiaoHangNhanhWard? ParseWard(JsonElement item)
    {
        var code = ReadAnyString(item, "WardCode", "ward_code");
        var districtId = ReadAnyInt(item, "DistrictID", "district_id");
        var name = ReadAnyString(item, "WardName", "ward_name");
        return !string.IsNullOrWhiteSpace(code) && districtId.HasValue && !string.IsNullOrWhiteSpace(name)
            ? new GiaoHangNhanhWard(
                code,
                districtId.Value,
                name,
                ReadAnyInt(item, "SupportType", "support_type"),
                ReadAnyInt(item, "Status", "status"))
            : null;
    }

    private static object BuildItem(GiaoHangNhanhPackage package) => new
    {
        name = package.Name,
        quantity = package.Quantity,
        price = package.InsuranceValue,
        length = package.LengthCm,
        width = package.WidthCm,
        height = package.HeightCm,
        weight = package.WeightGrams,
    };

    private Uri BuildApiUri(string path) => new(new Uri(EnsureTrailingSlash(_options.ApiBaseUrl)), path.TrimStart('/'));

    private string BuildTrackingUrl(string orderCode) =>
        string.IsNullOrWhiteSpace(_options.TrackingUrlTemplate)
            ? string.Empty
            : _options.TrackingUrlTemplate.Replace("{orderCode}", Uri.EscapeDataString(orderCode));

    private static JsonElement GetLatestLog(JsonElement data)
    {
        if (!TryGetProperty(data, "log", out var log) || log.ValueKind != JsonValueKind.Array)
        {
            return default;
        }

        JsonElement latest = default;
        foreach (var item in log.EnumerateArray())
        {
            latest = item;
        }

        return latest;
    }

    private static string EnsureTrailingSlash(string value) =>
        value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";

    private static string BuildHttpError(System.Net.HttpStatusCode statusCode, string? raw)
    {
        var message = ReadGhnApiError(raw) ?? (string.IsNullOrWhiteSpace(raw) ? "không có nội dung lỗi" : raw);
        return $"GHN trả về {(int)statusCode}: {message}";
    }

    private static string? ReadGhnApiError(string? rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(rawPayload);
            var code = ReadInt(json.RootElement, "code");
            if (code is null or 200)
            {
                return null;
            }

            var message = ReadString(json.RootElement, "message") ?? "GHN trả về lỗi.";
            var codeMessage = ReadString(json.RootElement, "code_message");
            return string.IsNullOrWhiteSpace(codeMessage) ? message : $"{message} ({codeMessage})";
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string? ReadString(JsonElement element, params string[] path)
    {
        if (!TryReadPath(element, path, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }

    private static string? ReadAnyString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var value = ReadString(element, name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static int? ReadInt(JsonElement element, params string[] path)
    {
        if (!TryReadPath(element, path, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
                ? number
                : null;
    }

    private static int? ReadAnyInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var value = ReadInt(element, name);
            if (value.HasValue)
            {
                return value;
            }
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement element, params string[] path)
    {
        if (!TryReadPath(element, path, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out number)
                ? number
                : null;
    }

    private static DateTime? ReadDateTime(JsonElement element, params string[] path)
    {
        if (!TryReadPath(element, path, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime)
            ? dateTime.ToUniversalTime()
            : null;
    }

    private static bool TryReadPath(JsonElement element, IReadOnlyList<string> path, out JsonElement value)
    {
        value = element;
        foreach (var name in path)
        {
            if (!TryGetProperty(value, name, out value))
            {
                return false;
            }
        }

        return true;
    }
}
