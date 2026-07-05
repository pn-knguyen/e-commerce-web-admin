using System.Globalization;
using System.Text.Json;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Integrations.GiaoHangNhanh;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Services.Shipping.Providers;
using e_commerce_web_admin.ViewModels.Shipments;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using e_commerce_web_admin.Services.Orders;

namespace e_commerce_web_admin.Services.Shipping;

public sealed class ShipmentAdminService : IShipmentAdminService
{
    private static readonly ShipmentStatus[] ActiveShipmentStatuses =
    [
        ShipmentStatus.Booked,
        ShipmentStatus.Booking,
        ShipmentStatus.ReadyToPick,
        ShipmentStatus.PickingUp,
        ShipmentStatus.Picking,
        ShipmentStatus.MoneyCollectPicking,
        ShipmentStatus.Picked,
        ShipmentStatus.InTransit,
        ShipmentStatus.Storing,
        ShipmentStatus.Transporting,
        ShipmentStatus.Sorting,
        ShipmentStatus.Delivering,
        ShipmentStatus.MoneyCollectDelivering,
        ShipmentStatus.Delivered,
        ShipmentStatus.DeliveryFail,
        ShipmentStatus.WaitingToReturn,
        ShipmentStatus.Return,
        ShipmentStatus.ReturnTransporting,
        ShipmentStatus.ReturnSorting,
        ShipmentStatus.Returning,
        ShipmentStatus.ReturnFail,
        ShipmentStatus.Returned,
        ShipmentStatus.Exception,
        ShipmentStatus.Damage,
        ShipmentStatus.Lost,
        ShipmentStatus.ProviderUnknown,
    ];

    private static readonly ShipmentStatus[] SyncableShipmentStatuses =
    [
        ShipmentStatus.Booked,
        ShipmentStatus.ReadyToPick,
        ShipmentStatus.PickingUp,
        ShipmentStatus.Picking,
        ShipmentStatus.MoneyCollectPicking,
        ShipmentStatus.Picked,
        ShipmentStatus.InTransit,
        ShipmentStatus.Storing,
        ShipmentStatus.Transporting,
        ShipmentStatus.Sorting,
        ShipmentStatus.Delivering,
        ShipmentStatus.MoneyCollectDelivering,
        ShipmentStatus.DeliveryFail,
        ShipmentStatus.WaitingToReturn,
        ShipmentStatus.Return,
        ShipmentStatus.ReturnTransporting,
        ShipmentStatus.ReturnSorting,
        ShipmentStatus.Returning,
        ShipmentStatus.ReturnFail,
        ShipmentStatus.Exception,
        ShipmentStatus.ProviderUnknown,
    ];

    private readonly ApplicationDbContext _db;
    private readonly IShippingProviderGateway _shippingProvider;
    private readonly GiaoHangNhanhOptions _options;

    public ShipmentAdminService(
        ApplicationDbContext db,
        IShippingProviderGateway shippingProvider,
        IOptions<GiaoHangNhanhOptions> options)
    {
        _db = db;
        _shippingProvider = shippingProvider;
        _options = options.Value;
    }

    public async Task<ShipmentPanelViewModel> GetPanelAsync(long orderId, CancellationToken ct = default)
    {
        var locations = await _db.FulfillmentLocations
            .AsNoTracking()
            .Where(location => location.IsActive)
            .OrderByDescending(location => location.IsDefault)
            .ThenBy(location => location.Name)
            .Select(location => new FulfillmentLocationOptionViewModel
            {
                Id = location.Id,
                Name = location.Name,
                ContactName = location.ContactName,
                Phone = location.Phone,
                Address = BuildAddress(location.DetailAddress, location.WardName, location.DistrictName, location.ProvinceName),
                IsDefault = location.IsDefault,
            })
            .ToListAsync(ct);

        var shipments = await _db.Shipments
            .AsNoTracking()
            .Where(item => item.OrderId == orderId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Select(item => new ShipmentSummaryViewModel
            {
                Id = item.Id,
                Status = item.Status,
                StatusLabel = ShipmentDisplay.GetStatusLabel(item.Status),
                StatusClass = ShipmentDisplay.GetStatusClass(item.Status),
                Provider = item.Provider.ToString(),
                ProviderDeliveryId = item.ProviderDeliveryId,
                ProviderStatus = item.ProviderStatus,
                TrackingUrl = item.TrackingUrl,
                QuotedFee = item.QuotedFee,
                ActualFee = item.ActualFee,
                Currency = item.Currency,
                PickupAddress = item.PickupAddress,
                DropoffAddress = item.DropoffAddress,
                FailureReason = item.FailureReason,
                CreatedAt = item.CreatedAt,
                BookedAt = item.BookedAt,
                PickedUpAt = item.PickedUpAt,
                DeliveredAt = item.DeliveredAt,
                CancelledAt = item.CancelledAt,
                LastSyncedAt = item.LastSyncedAt,
                Packages = item.Packages
                    .OrderBy(package => package.Sequence)
                    .Select(package => new ShipmentPackageSummaryViewModel
                    {
                        Description = package.Description,
                        Quantity = package.Quantity,
                        WeightGrams = package.WeightGrams,
                        DeclaredValue = package.DeclaredValue,
                    })
                    .ToList(),
                RecentEvents = item.Events
                    .OrderByDescending(shipmentEvent => shipmentEvent.OccurredAt)
                    .ThenByDescending(shipmentEvent => shipmentEvent.Id)
                    .Take(5)
                    .Select(shipmentEvent => new ShipmentEventSummaryViewModel
                    {
                        StatusLabel = ShipmentDisplay.GetStatusLabel(shipmentEvent.Status),
                        Message = shipmentEvent.Message,
                        OccurredAt = shipmentEvent.OccurredAt,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);

        foreach (var historyShipment in shipments.Where(item => !string.IsNullOrWhiteSpace(item.ProviderDeliveryId)))
        {
            historyShipment.TrackingUrl = BuildTrackingUrl(historyShipment.ProviderDeliveryId);
        }

        var shipment = shipments.FirstOrDefault();

        var order = await _db.Orders.AsNoTracking()
            .Include(item => item.ShippingAddress)
            .Where(item => item.Id == orderId)
            .Select(item => new
            {
                item.OrderCode,
                item.TotalAmount,
                item.ShippingProvince,
                item.ShippingWard,
                ShippingAddressProvinceName = item.ShippingAddress == null ? null : item.ShippingAddress.ProvinceName,
                ShippingAddressDistrictName = item.ShippingAddress == null ? null : item.ShippingAddress.DistrictName,
                ShippingAddressWardName = item.ShippingAddress == null ? null : item.ShippingAddress.WardName,
            })
            .FirstOrDefaultAsync(ct);

        return new ShipmentPanelViewModel
        {
            IsProviderConfigured = _shippingProvider.IsConfigured,
            FulfillmentLocations = locations,
            CurrentShipment = shipment,
            ShipmentHistory = shipments,
            QuoteForm = new ShipmentQuoteCreateViewModel
            {
                OrderId = orderId,
                FulfillmentLocationId = locations.FirstOrDefault(item => item.IsDefault)?.Id ?? locations.FirstOrDefault()?.Id,
                PackageDescription = order is null ? string.Empty : $"Đơn hàng {order.OrderCode}",
                ProviderDropoffProvinceName = order?.ShippingAddressProvinceName ?? order?.ShippingProvince,
                ProviderDropoffDistrictName = order?.ShippingAddressDistrictName,
                ProviderDropoffWardName = order?.ShippingAddressWardName ?? order?.ShippingWard,
                Quantity = 1,
            },
        };
    }

    public async Task<ShipmentActionResult> CreateQuoteAsync(
        long orderId,
        ShipmentQuoteCreateViewModel form,
        long? staffId,
        CancellationToken ct = default)
    {
        await RecoverStaleBookingClaimsAsync(orderId, ct);

        var order = await _db.Orders
            .Include(item => item.User)
            .Include(item => item.ShippingAddress)
            .Include(item => item.Shipments)
            .FirstOrDefaultAsync(item => item.Id == orderId, ct);
        if (order is null)
        {
            return ShipmentActionResult.NotFound("Không tìm thấy đơn hàng.");
        }

        if (order.OrderStatus is not OrderStatus.Confirmed and not OrderStatus.Processing)
        {
            return ShipmentActionResult.Failed("Chỉ lấy báo giá GHN cho đơn đã xác nhận hoặc đang xử lý.");
        }

        if (order.Shipments.Any(item => ActiveShipmentStatuses.Contains(item.Status)))
        {
            return ShipmentActionResult.Failed("Đơn hàng đã có vận đơn GHN đang hoạt động.");
        }

        var location = await _db.FulfillmentLocations
            .FirstOrDefaultAsync(item => item.Id == form.FulfillmentLocationId && item.IsActive, ct);
        if (location is null)
        {
            return ShipmentActionResult.Failed("Vui lòng chọn điểm lấy hàng đang hoạt động.");
        }

        var packageNumbers = ParsePackageNumbers(form);
        if (!packageNumbers.Succeeded)
        {
            return ShipmentActionResult.Failed(packageNumbers.Message!);
        }

        var shipment = new Shipment
        {
            OrderId = order.Id,
            FulfillmentLocationId = location.Id,
            Provider = ShippingProvider.GiaoHangNhanh,
            Status = ShipmentStatus.Draft,
            PickupContactName = location.ContactName,
            PickupPhone = location.Phone,
            PickupDetailAddress = location.DetailAddress,
            PickupAddress = BuildAddress(location.DetailAddress, location.WardName, location.DistrictName, location.ProvinceName),
            PickupLatitude = location.Latitude,
            PickupLongitude = location.Longitude,
            ProviderPickupProvinceCode = NormalizeOptional(location.ProvinceCode),
            ProviderPickupProvinceName = NormalizeOptional(location.ProvinceName),
            ProviderPickupDistrictCode = NormalizeOptional(location.DistrictCode),
            ProviderPickupDistrictName = NormalizeOptional(location.DistrictName),
            ProviderPickupWardCode = NormalizeOptional(location.WardCode),
            ProviderPickupWardName = NormalizeOptional(location.WardName),
            DropoffContactName = order.ShippingContactName,
            DropoffPhone = order.ShippingPhone,
            DropoffDetailAddress = order.ShippingDetail,
            DropoffAddress = BuildAddress(order.ShippingDetail, order.ShippingWard, order.ShippingProvince),
            DropoffLatitude = order.ShippingAddress?.Latitude,
            DropoffLongitude = order.ShippingAddress?.Longitude,
            ProviderDropoffProvinceCode = NormalizeOptional(form.ProviderDropoffProvinceCode),
            ProviderDropoffProvinceName = NormalizeOptional(form.ProviderDropoffProvinceName),
            ProviderDropoffDistrictCode = NormalizeOptional(form.ProviderDropoffDistrictCode),
            ProviderDropoffDistrictName = NormalizeOptional(form.ProviderDropoffDistrictName),
            ProviderDropoffWardCode = NormalizeOptional(form.ProviderDropoffWardCode),
            ProviderDropoffWardName = NormalizeOptional(form.ProviderDropoffWardName),
            Currency = "VND",
            RequestedByStaffId = staffId,
            CreatedAt = DateTime.UtcNow,
            Packages =
            [
                new ShipmentPackage
                {
                    Sequence = 1,
                    Description = form.PackageDescription.Trim(),
                    Quantity = Math.Max(1, form.Quantity),
                    WeightGrams = form.WeightGrams,
                    LengthCm = packageNumbers.LengthCm,
                    WidthCm = packageNumbers.WidthCm,
                    HeightCm = packageNumbers.HeightCm,
                    DeclaredValue = packageNumbers.DeclaredValue,
                    IsFragile = form.IsFragile,
                    Notes = form.Notes?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                },
            ],
        };

        var quoteRequest = BuildQuoteRequest(order, shipment);
        if (!quoteRequest.Succeeded)
        {
            return ShipmentActionResult.Failed(quoteRequest.Message!);
        }

        var quoteResponse = await _shippingProvider.CreateQuoteAsync(quoteRequest.Value!, ct);
        if (!quoteResponse.Succeeded || quoteResponse.Fee is null)
        {
            return ShipmentActionResult.Failed(quoteResponse.ErrorMessage ?? "GHN không trả về báo giá hợp lệ.");
        }

        var storedShipment = await FindOpenShipmentAsync(order.Id, ct);
        var insertedNewShipment = false;

        if (storedShipment is null)
        {
            insertedNewShipment = true;
            storedShipment = shipment;
            _db.Shipments.Add(storedShipment);
        }
        else
        {
            ApplyQuoteSnapshot(storedShipment, shipment);
            ReplacePackageSnapshot(storedShipment, shipment.Packages.OrderBy(item => item.Sequence).First());
        }

        ApplyQuoteResponse(storedShipment, quoteResponse);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (insertedNewShipment && IsUniqueConstraintViolation(ex))
        {
            foreach (var package in shipment.Packages)
            {
                _db.Entry(package).State = EntityState.Detached;
            }

            _db.Entry(shipment).State = EntityState.Detached;
            storedShipment = await FindOpenShipmentAsync(order.Id, ct);
            if (storedShipment is null)
            {
                return ShipmentActionResult.Failed(
                    "Vận đơn đang được xử lý hoặc đã được tạo trong lúc lấy báo giá. Vui lòng tải lại đơn hàng.");
            }

            ApplyQuoteSnapshot(storedShipment, shipment);
            ReplacePackageSnapshot(storedShipment, shipment.Packages.OrderBy(item => item.Sequence).First());
            ApplyQuoteResponse(storedShipment, quoteResponse);
            await _db.SaveChangesAsync(ct);
        }

        return ShipmentActionResult.Success($"Đã lấy báo giá GHN: {quoteResponse.Fee.Value:N0} {quoteResponse.Currency}.");
    }

    public async Task<ShipmentActionResult> BookShipmentAsync(
        long orderId,
        long shipmentId,
        CancellationToken ct = default)
    {
        await RecoverStaleBookingClaimsAsync(orderId, ct);

        var shipment = await _db.Shipments
            .Include(item => item.Order)
            .ThenInclude(order => order!.User)
            .Include(item => item.Order)
            .ThenInclude(order => order!.ShippingAddress)
            .Include(item => item.Packages)
            .FirstOrDefaultAsync(item => item.Id == shipmentId && item.OrderId == orderId, ct);
        if (shipment?.Order is null)
        {
            return ShipmentActionResult.NotFound("Không tìm thấy vận đơn.");
        }

        if (shipment.Status != ShipmentStatus.Quoted)
        {
            return ShipmentActionResult.Failed("Chỉ tạo vận đơn GHN sau khi đã có báo giá.");
        }

        var deliveryRequest = BuildDeliveryRequest(shipment.Order, shipment);
        if (!deliveryRequest.Succeeded)
        {
            return ShipmentActionResult.Failed(deliveryRequest.Message!);
        }

        var claimTime = DateTime.UtcNow;
        var claimed = await _db.Shipments
            .Where(item =>
                item.Id == shipment.Id &&
                item.OrderId == orderId &&
                item.Status == ShipmentStatus.Quoted &&
                item.ProviderDeliveryId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, ShipmentStatus.Booking)
                .SetProperty(item => item.UpdatedAt, claimTime),
                ct);
        if (claimed == 0)
        {
            return ShipmentActionResult.Failed("Vận đơn đang được xử lý hoặc đã được tạo bởi thao tác khác.");
        }

        shipment.Status = ShipmentStatus.Booking;
        shipment.UpdatedAt = claimTime;

        var deliveryResponse = await _shippingProvider.CreateOrderAsync(deliveryRequest.Value!, ct);
        if (!deliveryResponse.Succeeded || string.IsNullOrWhiteSpace(deliveryResponse.OrderCode))
        {
            shipment.Status = ShipmentStatus.Failed;
            shipment.FailureReason = deliveryResponse.ErrorMessage;
            shipment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return ShipmentActionResult.Failed(deliveryResponse.ErrorMessage ?? "GHN không tạo được vận đơn.");
        }

        var now = DateTime.UtcNow;
        shipment.Status = ShipmentStatusMapper.FromGiaoHangNhanhStatus(deliveryResponse.ProviderStatus);
        shipment.ProviderDeliveryId = deliveryResponse.OrderCode;
        shipment.ProviderStatus = deliveryResponse.ProviderStatus;
        shipment.TrackingUrl = deliveryResponse.TrackingUrl;
        shipment.ActualFee = deliveryResponse.Fee ?? shipment.QuotedFee;
        shipment.Currency = deliveryResponse.Currency;
        shipment.BookedAt = now;
        shipment.FailureReason = null;
        shipment.UpdatedAt = now;

        shipment.Order.OrderStatus = OrderStatus.Shipping;
        shipment.Order.UpdatedAt = now;

        _db.ShipmentEvents.Add(new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            ProviderEventId = $"local:booked:{shipment.Id}",
            ProviderStatus = shipment.ProviderStatus,
            Status = shipment.Status,
            Message = $"Đã tạo vận đơn GHN {deliveryResponse.OrderCode}.",
            OccurredAt = now,
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(ct);
        return ShipmentActionResult.Success($"Đã tạo vận đơn GHN cho đơn {shipment.Order.OrderCode}.");
    }

    public async Task<ShipmentActionResult> CancelShipmentAsync(
        long orderId,
        long shipmentId,
        CancellationToken ct = default)
    {
        var shipment = await _db.Shipments
            .Include(item => item.Order)
            .FirstOrDefaultAsync(item => item.Id == shipmentId && item.OrderId == orderId, ct);
        if (shipment?.Order is null)
        {
            return ShipmentActionResult.NotFound("Không tìm thấy vận đơn.");
        }

        if (!ShipmentDisplay.CanCancel(shipment.Status))
        {
            return ShipmentActionResult.Failed("Trạng thái vận đơn hiện tại không thể hủy trên GHN.");
        }

        if (!string.IsNullOrWhiteSpace(shipment.ProviderDeliveryId))
        {
            var cancelResponse = await _shippingProvider.CancelOrderAsync(shipment.ProviderDeliveryId, ct);
            if (!cancelResponse.Succeeded)
            {
                return ShipmentActionResult.Failed(cancelResponse.ErrorMessage ?? "GHN không hủy được vận đơn.");
            }
        }

        var now = DateTime.UtcNow;
        shipment.Status = ShipmentStatus.Cancelled;
        shipment.ProviderStatus = "CANCELLED";
        shipment.CancelledAt = now;
        shipment.UpdatedAt = now;

        _db.ShipmentEvents.Add(new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            ProviderEventId = $"local:cancelled:{shipment.Id}",
            ProviderStatus = shipment.ProviderStatus,
            Status = shipment.Status,
            Message = string.IsNullOrWhiteSpace(shipment.ProviderDeliveryId)
                ? "Đã hủy vận đơn GHN."
                : $"Đã hủy vận đơn GHN {shipment.ProviderDeliveryId}.",
            OccurredAt = now,
            CreatedAt = now,
        });

        var hasOtherActiveShipment = await _db.Shipments.AnyAsync(item =>
            item.OrderId == orderId &&
            item.Id != shipment.Id &&
            ActiveShipmentStatuses.Contains(item.Status),
            ct);

        if (!hasOtherActiveShipment && shipment.Order.OrderStatus == OrderStatus.Shipping)
        {
            shipment.Order.OrderStatus = OrderStatus.Processing;
            shipment.Order.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return ShipmentActionResult.Success("Đã hủy vận đơn GHN.");
    }

    public async Task<ShipmentActionResult> SyncShipmentStatusAsync(
        long orderId,
        long shipmentId,
        CancellationToken ct = default)
    {
        var shipment = await _db.Shipments
            .Include(item => item.Order)
            .FirstOrDefaultAsync(item => item.Id == shipmentId && item.OrderId == orderId, ct);
        if (shipment?.Order is null)
        {
            return ShipmentActionResult.NotFound("Khong tim thay van don.");
        }

        var result = await SyncTrackedShipmentStatusAsync(shipment, ct);
        if (!result.Succeeded)
        {
            return result;
        }

        return ShipmentActionResult.Success($"Da dong bo trang thai GHN: {ShipmentDisplay.GetStatusLabel(shipment.Status)}.");
    }

    public async Task<int> SyncActiveProviderStatusesAsync(CancellationToken ct = default)
    {
        var recoveredCount = await RecoverStaleBookingClaimsAsync(orderId: null, ct: ct);
        if (!_shippingProvider.IsConfigured)
        {
            return recoveredCount;
        }

        var reconciledCount = await SyncOrderStatusesFromLatestShipmentsAsync(ct);
        var batchSize = Math.Clamp(_options.StatusSyncBatchSize, 1, 100);
        var shipments = await _db.Shipments
            .Include(item => item.Order)
            .Where(item =>
                item.Provider == ShippingProvider.GiaoHangNhanh &&
                item.ProviderDeliveryId != null &&
                SyncableShipmentStatuses.Contains(item.Status))
            .OrderBy(item => item.LastSyncedAt ?? item.CreatedAt)
            .ThenBy(item => item.Id)
            .Take(batchSize)
            .ToListAsync(ct);

        var syncedCount = 0;
        foreach (var shipment in shipments)
        {
            var result = await SyncTrackedShipmentStatusAsync(shipment, ct);
            if (result.Succeeded)
            {
                syncedCount++;
            }
        }

        return syncedCount + reconciledCount + recoveredCount;
    }

    private async Task<int> SyncOrderStatusesFromLatestShipmentsAsync(CancellationToken ct)
    {
        var takeCount = Math.Clamp(_options.StatusSyncBatchSize * 2, 20, 200);
        var latestShipmentIds = _db.Shipments
            .Where(item =>
                item.Provider == ShippingProvider.GiaoHangNhanh &&
                item.ProviderDeliveryId != null)
            .GroupBy(item => item.OrderId)
            .Select(group => group.Max(item => item.Id));

        var candidates = await _db.Shipments
            .Include(item => item.Order)
            .ThenInclude(order => order!.PaymentMethod)
            .Where(item =>
                latestShipmentIds.Contains(item.Id) &&
                item.Order != null &&
                ((item.Status == ShipmentStatus.Delivered &&
                    (item.Order.OrderStatus != OrderStatus.Completed ||
                        (item.Order.PaymentStatus != PaymentStatus.Paid &&
                            item.Order.PaymentStatus != PaymentStatus.Refunded &&
                            (item.Order.PaymentMethodId == PaymentMethodIds.CashOnDelivery ||
                                (item.Order.PaymentMethod != null &&
                                    (item.Order.PaymentMethod.Name.Contains("COD") ||
                                        item.Order.PaymentMethod.Name.Contains("nhận hàng"))))))) ||
                 ((item.Status == ShipmentStatus.Returned ||
                    item.Status == ShipmentStatus.Damage ||
                    item.Status == ShipmentStatus.Lost) &&
                    item.Order.OrderStatus != OrderStatus.Returned)))
            .OrderBy(item => item.Id)
            .Take(takeCount)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var changedCount = 0;
        foreach (var shipment in candidates
            .GroupBy(item => item.OrderId)
            .Select(group => group.First()))
        {
            var beforeOrderStatus = shipment.Order?.OrderStatus;
            var beforePaymentStatus = shipment.Order?.PaymentStatus;
            ShipmentStatusMapper.SyncOrderStatusFromShipment(shipment.Order, shipment.Status, now);
            if ((beforeOrderStatus.HasValue && shipment.Order?.OrderStatus != beforeOrderStatus.Value) ||
                (beforePaymentStatus.HasValue && shipment.Order?.PaymentStatus != beforePaymentStatus.Value))
            {
                changedCount++;
                if (beforeOrderStatus.HasValue && beforeOrderStatus.Value != shipment.Order!.OrderStatus &&
                    shipment.Order.OrderStatus is OrderStatus.Completed or OrderStatus.Returned or OrderStatus.Cancelled)
                {
                    await _db.Entry(shipment.Order).Collection(o => o.OrderItems).Query()
                        .Include(i => i.ProductVariant).ThenInclude(v => v!.Product).LoadAsync(ct);
                    var fifoResult = await OrderFifoCostHelper.ApplyStatusChangeAsync(
                        _db,
                        shipment.Order,
                        beforeOrderStatus.Value,
                        shipment.Order.OrderStatus,
                        ct);
                    if (!fifoResult.Succeeded)
                    {
                        shipment.Order.OrderStatus = beforeOrderStatus.Value;
                    }
                }
            }
        }

        if (changedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        return changedCount;
    }

    public async Task<ShipmentActionResult> HandleProviderWebhookAsync(string rawPayload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return ShipmentActionResult.Failed("Webhook không có payload.");
        }

        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(rawPayload);
        }
        catch (JsonException)
        {
            return ShipmentActionResult.Failed("Webhook GHN không đúng định dạng JSON.");
        }

        using (json)
        {
        var root = json.RootElement;
        var deliveryId = ReadAnyString(root, "OrderCode", "order_code", "orderCode");
        if (string.IsNullOrWhiteSpace(deliveryId))
        {
            return ShipmentActionResult.Failed("Webhook không có delivery ID.");
        }

        var shipment = await _db.Shipments
            .Include(item => item.Order)
            .FirstOrDefaultAsync(item => item.ProviderDeliveryId == deliveryId, ct);
        if (shipment?.Order is null)
        {
            return ShipmentActionResult.NotFound("Không tìm thấy vận đơn từ webhook GHN.");
        }

        var providerEventId = ReadAnyString(root, "OrderCode", "order_code", "orderCode") is { } orderCode
            ? $"{orderCode}:{ReadAnyString(root, "Status", "status")}:{ReadAnyString(root, "Time", "time")}"
            : null;
        if (!string.IsNullOrWhiteSpace(providerEventId) &&
            await _db.ShipmentEvents.AnyAsync(item => item.ProviderEventId == providerEventId, ct))
        {
            return ShipmentActionResult.Success("Webhook GHN đã được xử lý trước đó.");
        }

        var providerStatus = ReadAnyString(root, "Status", "status", "OrderStatus", "order_status") ?? shipment.ProviderStatus;
        if (string.IsNullOrWhiteSpace(providerStatus))
        {
            return ShipmentActionResult.Failed("Webhook GHN không có trạng thái vận đơn.");
        }

        var nextStatus = ShipmentStatusMapper.FromGiaoHangNhanhStatus(providerStatus);
        var occurredAt = ReadDateTime(root, "Time", "time", "timestamp", "occurredAt", "updatedAt") ?? DateTime.UtcNow;

        var beforeOrderStatus = shipment.Order.OrderStatus;

        shipment.ProviderStatus = providerStatus;
        shipment.Status = nextStatus;
        shipment.LastSyncedAt = DateTime.UtcNow;
        shipment.UpdatedAt = DateTime.UtcNow;
        ShipmentStatusMapper.SyncOrderStatusFromShipment(shipment.Order, nextStatus, DateTime.UtcNow);

        if (beforeOrderStatus != shipment.Order.OrderStatus &&
            shipment.Order.OrderStatus is OrderStatus.Completed or OrderStatus.Returned or OrderStatus.Cancelled)
        {
            await _db.Entry(shipment.Order).Collection(o => o.OrderItems).Query()
                .Include(i => i.ProductVariant).ThenInclude(v => v!.Product).LoadAsync(ct);
            var fifoResult = await OrderFifoCostHelper.ApplyStatusChangeAsync(
                _db,
                shipment.Order,
                beforeOrderStatus,
                shipment.Order.OrderStatus,
                ct);
            if (!fifoResult.Succeeded)
            {
                return ShipmentActionResult.Failed(fifoResult.ErrorMessage ?? "Không thể ghi nhận giá vốn FIFO.");
            }
        }

        if (ShipmentStatusMapper.IsPickupProgressStatus(nextStatus) && shipment.PickedUpAt is null)
        {
            shipment.PickedUpAt = occurredAt;
        }
        else if (nextStatus == ShipmentStatus.Delivered)
        {
            shipment.DeliveredAt ??= occurredAt;
        }
        else if (nextStatus == ShipmentStatus.Cancelled)
        {
            shipment.CancelledAt ??= occurredAt;
        }
        else if (ShipmentStatusMapper.IsFailureStatus(nextStatus) && !string.IsNullOrWhiteSpace(ReadAnyString(root, "Description", "description", "Reason", "reason")))
        {
            shipment.FailureReason = ReadAnyString(root, "Description", "description", "Reason", "reason");
        }

        _db.ShipmentEvents.Add(new ShipmentEvent
        {
            ShipmentId = shipment.Id,
            ProviderEventId = providerEventId,
            ProviderStatus = providerStatus,
            Status = nextStatus,
            Message = ReadAnyString(root, "Description", "description", "Reason", "reason"),
            DriverName = ReadString(root, "courier", "name") ?? ReadString(root, "driverName"),
            DriverPhone = ReadString(root, "courier", "phone") ?? ReadString(root, "driverPhone"),
            VehiclePlate = ReadString(root, "courier", "vehiclePlate") ?? ReadString(root, "vehiclePlate"),
            OccurredAt = occurredAt,
            RawPayloadJson = rawPayload,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        return ShipmentActionResult.Success("Đã cập nhật webhook GHN.");
        }
    }

    private async Task<ShipmentActionResult> SyncTrackedShipmentStatusAsync(
        Shipment shipment,
        CancellationToken ct)
    {
        if (shipment.Provider != ShippingProvider.GiaoHangNhanh)
        {
            return ShipmentActionResult.Failed("Van don nay khong thuoc GHN.");
        }

        if (string.IsNullOrWhiteSpace(shipment.ProviderDeliveryId))
        {
            return ShipmentActionResult.Failed("Van don chua co ma GHN.");
        }

        var detail = await _shippingProvider.GetOrderDetailAsync(shipment.ProviderDeliveryId, ct);
        if (!detail.Succeeded)
        {
            return ShipmentActionResult.Failed(detail.ErrorMessage ?? "Khong dong bo duoc trang thai GHN.");
        }

        return await ApplyProviderStatusAsync(
            shipment,
            detail.ProviderStatus,
            detail.UpdatedAt,
            detail.Message,
            detail.RawPayloadJson,
            "ghn-detail",
            detail.Fee,
            ct);
    }

    private async Task<ShipmentActionResult> ApplyProviderStatusAsync(
        Shipment shipment,
        string? providerStatus,
        DateTime? providerOccurredAt,
        string? message,
        string? rawPayload,
        string source,
        decimal? actualFee,
        CancellationToken ct)
    {
        providerStatus = NormalizeOptional(providerStatus) ?? shipment.ProviderStatus;
        if (string.IsNullOrWhiteSpace(providerStatus))
        {
            return ShipmentActionResult.Failed("GHN khong tra ve trang thai van don.");
        }

        var nextStatus = ShipmentStatusMapper.FromGiaoHangNhanhStatus(providerStatus);
        var occurredAt = providerOccurredAt ?? DateTime.UtcNow;
        var now = DateTime.UtcNow;

        var beforeOrderStatus = shipment.Order?.OrderStatus;

        shipment.ProviderStatus = providerStatus;
        shipment.Status = nextStatus;
        shipment.LastSyncedAt = now;
        shipment.UpdatedAt = now;
        ShipmentStatusMapper.SyncOrderStatusFromShipment(shipment.Order, nextStatus, now);

        if (beforeOrderStatus.HasValue && beforeOrderStatus.Value != shipment.Order!.OrderStatus &&
            shipment.Order.OrderStatus is OrderStatus.Completed or OrderStatus.Returned or OrderStatus.Cancelled)
        {
            await _db.Entry(shipment.Order).Collection(o => o.OrderItems).Query()
                .Include(i => i.ProductVariant).ThenInclude(v => v!.Product).LoadAsync(ct);
            var fifoResult = await OrderFifoCostHelper.ApplyStatusChangeAsync(
                _db,
                shipment.Order,
                beforeOrderStatus.Value,
                shipment.Order.OrderStatus,
                ct);
            if (!fifoResult.Succeeded)
            {
                return ShipmentActionResult.Failed(fifoResult.ErrorMessage ?? "Khong the ghi nhan gia von FIFO.");
            }
        }

        if (actualFee.HasValue)
        {
            shipment.ActualFee = actualFee.Value;
        }

        if (ShipmentStatusMapper.IsPickupProgressStatus(nextStatus) && shipment.PickedUpAt is null)
        {
            shipment.PickedUpAt = occurredAt;
        }
        else if (nextStatus == ShipmentStatus.Delivered)
        {
            shipment.DeliveredAt ??= occurredAt;
        }
        else if (nextStatus == ShipmentStatus.Cancelled)
        {
            shipment.CancelledAt ??= occurredAt;
        }
        else if (ShipmentStatusMapper.IsFailureStatus(nextStatus) && !string.IsNullOrWhiteSpace(message))
        {
            shipment.FailureReason = message.Trim();
        }

        var providerEventId = BuildProviderEventId(
            source,
            shipment.ProviderDeliveryId,
            providerStatus,
            providerOccurredAt);
        var existingEvent = string.IsNullOrWhiteSpace(providerEventId)
            ? null
            : await _db.ShipmentEvents.FirstOrDefaultAsync(item => item.ProviderEventId == providerEventId, ct);

        if (existingEvent is null)
        {
            _db.ShipmentEvents.Add(new ShipmentEvent
            {
                ShipmentId = shipment.Id,
                ProviderEventId = providerEventId,
                ProviderStatus = providerStatus,
                Status = nextStatus,
                Message = message,
                OccurredAt = occurredAt,
                RawPayloadJson = rawPayload,
                CreatedAt = now,
            });
        }
        else
        {
            existingEvent.ProviderStatus = providerStatus;
            existingEvent.Status = nextStatus;
            existingEvent.Message = string.IsNullOrWhiteSpace(message) ? existingEvent.Message : message;
            existingEvent.OccurredAt = occurredAt;
            existingEvent.RawPayloadJson = string.IsNullOrWhiteSpace(rawPayload) ? existingEvent.RawPayloadJson : rawPayload;
        }

        await _db.SaveChangesAsync(ct);
        return ShipmentActionResult.Success("Da dong bo trang thai GHN.");
    }

    private Task<Shipment?> FindOpenShipmentAsync(long orderId, CancellationToken ct) =>
        _db.Shipments
            .Include(item => item.Packages)
            .Where(item =>
                item.OrderId == orderId &&
                item.Provider == ShippingProvider.GiaoHangNhanh &&
                item.ProviderDeliveryId == null &&
                (item.Status == ShipmentStatus.Draft ||
                    item.Status == ShipmentStatus.Quoted ||
                    item.Status == ShipmentStatus.Failed))
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<int> RecoverStaleBookingClaimsAsync(long? orderId, CancellationToken ct)
    {
        var recoveryMinutes = Math.Clamp(_options.BookingRecoveryMinutes, 1, 60);
        var staleBefore = DateTime.UtcNow.AddMinutes(-recoveryMinutes);
        var recoveredAt = DateTime.UtcNow;
        var query = _db.Shipments.Where(item =>
            item.Provider == ShippingProvider.GiaoHangNhanh &&
            item.ProviderDeliveryId == null &&
            item.Status == ShipmentStatus.Booking &&
            (item.UpdatedAt ?? item.CreatedAt) <= staleBefore);

        if (orderId.HasValue)
        {
            query = query.Where(item => item.OrderId == orderId.Value);
        }

        return await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(item => item.Status, ShipmentStatus.Quoted)
            .SetProperty(
                item => item.FailureReason,
                $"Lần tạo vận đơn trước bị gián đoạn. Có thể thử tạo lại an toàn sau {recoveryMinutes} phút.")
            .SetProperty(item => item.UpdatedAt, recoveredAt),
            ct);
    }

    private void ReplacePackageSnapshot(Shipment shipment, ShipmentPackage sourcePackage)
    {
        var storedPackage = shipment.Packages.OrderBy(item => item.Sequence).FirstOrDefault();
        if (storedPackage is null)
        {
            storedPackage = ClonePackage(sourcePackage);
            shipment.Packages.Add(storedPackage);
        }
        else
        {
            ApplyPackageSnapshot(storedPackage, sourcePackage);
        }

        foreach (var extraPackage in shipment.Packages
            .Where(item => !ReferenceEquals(item, storedPackage) && item.Id != storedPackage.Id)
            .ToList())
        {
            _db.ShipmentPackages.Remove(extraPackage);
        }
    }

    private static void ApplyQuoteResponse(
        Shipment shipment,
        ShippingProviderQuoteResponse quoteResponse)
    {
        shipment.Status = ShipmentStatus.Quoted;
        shipment.ProviderQuoteId = quoteResponse.ProviderQuoteId;
        shipment.ProviderStatus = null;
        shipment.TrackingUrl = null;
        shipment.QuotedFee = quoteResponse.Fee;
        shipment.ActualFee = null;
        shipment.Currency = quoteResponse.Currency;
        shipment.FailureReason = null;
        shipment.UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static void ApplyQuoteSnapshot(Shipment target, Shipment source)
    {
        target.FulfillmentLocationId = source.FulfillmentLocationId;
        target.Provider = source.Provider;
        target.PickupContactName = source.PickupContactName;
        target.PickupPhone = source.PickupPhone;
        target.PickupDetailAddress = source.PickupDetailAddress;
        target.PickupAddress = source.PickupAddress;
        target.PickupLatitude = source.PickupLatitude;
        target.PickupLongitude = source.PickupLongitude;
        target.ProviderPickupProvinceCode = source.ProviderPickupProvinceCode;
        target.ProviderPickupProvinceName = source.ProviderPickupProvinceName;
        target.ProviderPickupDistrictCode = source.ProviderPickupDistrictCode;
        target.ProviderPickupDistrictName = source.ProviderPickupDistrictName;
        target.ProviderPickupWardCode = source.ProviderPickupWardCode;
        target.ProviderPickupWardName = source.ProviderPickupWardName;
        target.DropoffContactName = source.DropoffContactName;
        target.DropoffPhone = source.DropoffPhone;
        target.DropoffDetailAddress = source.DropoffDetailAddress;
        target.DropoffAddress = source.DropoffAddress;
        target.DropoffLatitude = source.DropoffLatitude;
        target.DropoffLongitude = source.DropoffLongitude;
        target.ProviderDropoffProvinceCode = source.ProviderDropoffProvinceCode;
        target.ProviderDropoffProvinceName = source.ProviderDropoffProvinceName;
        target.ProviderDropoffDistrictCode = source.ProviderDropoffDistrictCode;
        target.ProviderDropoffDistrictName = source.ProviderDropoffDistrictName;
        target.ProviderDropoffWardCode = source.ProviderDropoffWardCode;
        target.ProviderDropoffWardName = source.ProviderDropoffWardName;
        target.Currency = source.Currency;
        target.RequestedByStaffId = source.RequestedByStaffId;
    }

    private static ShipmentPackage ClonePackage(ShipmentPackage source)
    {
        return new ShipmentPackage
        {
            Sequence = source.Sequence,
            Description = source.Description,
            Quantity = source.Quantity,
            WeightGrams = source.WeightGrams,
            LengthCm = source.LengthCm,
            WidthCm = source.WidthCm,
            HeightCm = source.HeightCm,
            DeclaredValue = source.DeclaredValue,
            IsFragile = source.IsFragile,
            Notes = source.Notes,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static void ApplyPackageSnapshot(ShipmentPackage target, ShipmentPackage source)
    {
        target.Sequence = source.Sequence;
        target.Description = source.Description;
        target.Quantity = source.Quantity;
        target.WeightGrams = source.WeightGrams;
        target.LengthCm = source.LengthCm;
        target.WidthCm = source.WidthCm;
        target.HeightCm = source.HeightCm;
        target.DeclaredValue = source.DeclaredValue;
        target.IsFragile = source.IsFragile;
        target.Notes = source.Notes;
        target.UpdatedAt = DateTime.UtcNow;
    }

    private static string? BuildProviderEventId(
        string source,
        string? deliveryId,
        string? providerStatus,
        DateTime? providerOccurredAt)
    {
        if (string.IsNullOrWhiteSpace(deliveryId) || string.IsNullOrWhiteSpace(providerStatus))
        {
            return null;
        }

        var timeKey = providerOccurredAt?.ToString("O", CultureInfo.InvariantCulture) ?? "no-time";
        return $"{source}:{deliveryId.Trim()}:{providerStatus.Trim()}:{timeKey}";
    }

    private RequestBuildResult<ShippingProviderQuoteRequest> BuildQuoteRequest(
        Order order,
        Shipment shipment)
    {
        var deliveryAddress = GetProviderDropoffAddress(shipment);
        if (!deliveryAddress.Succeeded)
        {
            return RequestBuildResult<ShippingProviderQuoteRequest>.Failed(deliveryAddress.Message!);
        }

        var pickupAddress = GetProviderPickupAddress(shipment);
        if (!pickupAddress.Succeeded)
        {
            return RequestBuildResult<ShippingProviderQuoteRequest>.Failed(pickupAddress.Message!);
        }

        var package = shipment.Packages.Select(BuildPackage).ToList();
        if (package.Count == 0)
        {
            return RequestBuildResult<ShippingProviderQuoteRequest>.Failed("Vui lòng nhập ít nhất một kiện hàng.");
        }

        return RequestBuildResult<ShippingProviderQuoteRequest>.Success(new ShippingProviderQuoteRequest(
            order.OrderCode,
            pickupAddress.DistrictId,
            pickupAddress.WardCode,
            deliveryAddress.DistrictId!.Value,
            deliveryAddress.WardCode!,
            _options.ServiceId,
            _options.ServiceTypeId,
            GetCodAmount(order),
            _options.Coupon,
            package));
    }

    private RequestBuildResult<ShippingProviderCreateOrderRequest> BuildDeliveryRequest(Order order, Shipment shipment)
    {
        var deliveryAddress = GetProviderDropoffAddress(shipment);
        if (!deliveryAddress.Succeeded)
        {
            return RequestBuildResult<ShippingProviderCreateOrderRequest>.Failed(deliveryAddress.Message!);
        }

        var pickupLocation = new FulfillmentLocation
        {
            ContactName = shipment.PickupContactName,
            Phone = shipment.PickupPhone,
            DetailAddress = shipment.PickupDetailAddress ?? shipment.PickupAddress,
            WardName = shipment.ProviderPickupWardName ?? string.Empty,
            DistrictName = shipment.ProviderPickupDistrictName,
            ProvinceName = shipment.ProviderPickupProvinceName ?? string.Empty,
        };

        if (string.IsNullOrWhiteSpace(pickupLocation.ContactName))
        {
            return RequestBuildResult<ShippingProviderCreateOrderRequest>.Failed("Điểm lấy hàng chưa có người phụ trách.");
        }

        if (string.IsNullOrWhiteSpace(pickupLocation.Phone))
        {
            return RequestBuildResult<ShippingProviderCreateOrderRequest>.Failed("Điểm lấy hàng chưa có số điện thoại.");
        }

        if (string.IsNullOrWhiteSpace(pickupLocation.DetailAddress))
        {
            return RequestBuildResult<ShippingProviderCreateOrderRequest>.Failed("Điểm lấy hàng chưa có địa chỉ chi tiết.");
        }

        if (string.IsNullOrWhiteSpace(pickupLocation.WardName))
        {
            return RequestBuildResult<ShippingProviderCreateOrderRequest>.Failed("Điểm lấy hàng chưa có tên phường/xã.");
        }

        if (string.IsNullOrWhiteSpace(pickupLocation.DistrictName))
        {
            return RequestBuildResult<ShippingProviderCreateOrderRequest>.Failed("Điểm lấy hàng chưa có tên quận/huyện.");
        }

        if (string.IsNullOrWhiteSpace(pickupLocation.ProvinceName))
        {
            return RequestBuildResult<ShippingProviderCreateOrderRequest>.Failed("Điểm lấy hàng chưa có tỉnh/thành.");
        }

        var pickupAddress = GetProviderPickupAddress(shipment);
        if (!pickupAddress.Succeeded)
        {
            return RequestBuildResult<ShippingProviderCreateOrderRequest>.Failed(pickupAddress.Message!);
        }

        var pickupDetailAddress = pickupLocation.DetailAddress.Trim();
        var dropoffDetailAddress = string.IsNullOrWhiteSpace(shipment.DropoffDetailAddress)
            ? shipment.DropoffAddress.Trim()
            : shipment.DropoffDetailAddress.Trim();
        var packages = shipment.Packages.Select(BuildPackage).ToList();
        if (packages.Count == 0)
        {
            return RequestBuildResult<ShippingProviderCreateOrderRequest>.Failed("Vui lòng nhập ít nhất một kiện hàng.");
        }

        return RequestBuildResult<ShippingProviderCreateOrderRequest>.Success(new ShippingProviderCreateOrderRequest(
            order.OrderCode,
            _options.PaymentTypeId,
            _options.RequiredNote,
            shipment.Packages.FirstOrDefault()?.Notes,
            pickupLocation.ContactName.Trim(),
            pickupLocation.Phone.Trim(),
            pickupDetailAddress,
            pickupLocation.WardName.Trim(),
            pickupLocation.DistrictName.Trim(),
            pickupLocation.ProvinceName.Trim(),
            pickupLocation.Phone.Trim(),
            pickupDetailAddress,
            pickupAddress.DistrictId,
            pickupAddress.WardCode,
            shipment.DropoffContactName,
            shipment.DropoffPhone,
            dropoffDetailAddress,
            deliveryAddress.DistrictId!.Value,
            deliveryAddress.WardCode!,
            GetCodAmount(order),
            string.IsNullOrWhiteSpace(packages[0].Description) ? $"Đơn hàng {order.OrderCode}" : packages[0].Description,
            _options.ServiceId,
            _options.ServiceTypeId,
            _options.Coupon,
            packages));
    }

    private ShippingProviderPackage BuildPackage(ShipmentPackage package)
    {
        var declaredValue = ToBoundedInt(package.DeclaredValue, 0, _options.MaxInsuranceValue);
        return new ShippingProviderPackage(
            $"Kiện {package.Sequence}",
            package.Description,
            Math.Max(1, package.Quantity),
            ToBoundedInt(package.WeightGrams, 1, 30_000, _options.DefaultWeightGrams),
            ToBoundedInt(package.LengthCm, 1, 150, _options.DefaultLengthCm),
            ToBoundedInt(package.WidthCm, 1, 150, _options.DefaultWidthCm),
            ToBoundedInt(package.HeightCm, 1, 150, _options.DefaultHeightCm),
            declaredValue);
    }

    private static string BuildAddress(params string?[] values) =>
        string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));

    private string? BuildTrackingUrl(string? orderCode) =>
        string.IsNullOrWhiteSpace(orderCode)
            ? null
            : _options.TrackingUrlTemplate.Replace("{orderCode}", Uri.EscapeDataString(orderCode.Trim()));

    private static AddressCodeResult GetProviderDropoffAddress(Shipment shipment)
    {
        if (!TryParsePositiveInt(shipment.ProviderDropoffDistrictCode, out var districtId))
        {
            return AddressCodeResult.Failed("Vui lòng chọn quận/huyện giao đến theo đơn vị vận chuyển.");
        }

        if (string.IsNullOrWhiteSpace(shipment.ProviderDropoffWardCode))
        {
            return AddressCodeResult.Failed("Vui lòng chọn phường/xã giao đến theo đơn vị vận chuyển.");
        }

        return AddressCodeResult.Success(districtId, shipment.ProviderDropoffWardCode.Trim());
    }

    private static AddressCodeResult GetProviderPickupAddress(Shipment shipment)
    {
        if (!TryParsePositiveInt(shipment.ProviderPickupDistrictCode, out var districtId))
        {
            return AddressCodeResult.Failed("Điểm lấy hàng chưa có mã quận/huyện theo đơn vị vận chuyển.");
        }

        if (string.IsNullOrWhiteSpace(shipment.ProviderPickupWardCode))
        {
            return AddressCodeResult.Failed("Điểm lấy hàng chưa có mã phường/xã theo đơn vị vận chuyển.");
        }

        return AddressCodeResult.Success(districtId, shipment.ProviderPickupWardCode.Trim());
    }

    private int GetCodAmount(Order order)
    {
        if (!_options.EnableCodForUnpaidOrders ||
            order.PaymentStatus == PaymentStatus.Paid ||
            !ShipmentStatusMapper.IsCashOnDelivery(order))
        {
            return 0;
        }

        return ToBoundedInt(order.TotalAmount, 0, 10_000_000);
    }

    private static int ToBoundedInt(int? value, int min, int max, int fallback) =>
        Math.Clamp(value.GetValueOrDefault(fallback), min, max);

    private static int ToBoundedInt(decimal? value, int min, int max, int fallback = 0)
    {
        var number = value.HasValue
            ? (int)Math.Round(value.Value, MidpointRounding.AwayFromZero)
            : fallback;
        return Math.Clamp(number, min, max);
    }

    private static bool TryParsePositiveInt(string? value, out int result) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result > 0;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private PackageNumberValues ParsePackageNumbers(ShipmentQuoteCreateViewModel form)
    {
        if (!form.WeightGrams.HasValue)
        {
            return PackageNumberValues.Failed("Vui lòng nhập cân nặng kiện hàng.");
        }

        if (form.WeightGrams is < 1 or > 30_000)
        {
            return PackageNumberValues.Failed("Cân nặng kiện hàng phải từ 1 đến 30000 gram.");
        }

        var length = ParseRequiredDecimal(form.LengthCm, "Chiều dài", 1m, 150m);
        if (!length.Succeeded)
        {
            return PackageNumberValues.Failed(length.Message!);
        }

        var width = ParseRequiredDecimal(form.WidthCm, "Chiều rộng", 1m, 150m);
        if (!width.Succeeded)
        {
            return PackageNumberValues.Failed(width.Message!);
        }

        var height = ParseRequiredDecimal(form.HeightCm, "Chiều cao", 1m, 150m);
        if (!height.Succeeded)
        {
            return PackageNumberValues.Failed(height.Message!);
        }

        var declaredValue = ParseOptionalNonNegativeDecimal(form.DeclaredValue, "Giá trị khai báo");
        if (!declaredValue.Succeeded)
        {
            return PackageNumberValues.Failed(declaredValue.Message!);
        }

        return PackageNumberValues.Success(length.Value, width.Value, height.Value, declaredValue.Value);
    }

    private static (bool Succeeded, decimal? Value, string? Message) ParseRequiredDecimal(
        string? value,
        string label,
        decimal min,
        decimal max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (false, null, $"Vui lòng nhập {label.ToLowerInvariant()}.");
        }

        if (!ShipmentFormNumberParser.TryParseDecimal(value, out var number))
        {
            return (false, null, $"{label} phải là số hợp lệ.");
        }

        if (number < min || number > max)
        {
            return (false, null, $"{label} phải từ {ShipmentFormNumberParser.Format(min)} đến {ShipmentFormNumberParser.Format(max)}.");
        }

        return (true, number, null);
    }

    private static (bool Succeeded, decimal? Value, string? Message) ParseOptionalNonNegativeDecimal(
        string? value,
        string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (true, null, null);
        }

        if (!ShipmentFormNumberParser.TryParseDecimal(value, out var number))
        {
            return (false, null, $"{label} phải là số hợp lệ.");
        }

        if (number < 0)
        {
            return (false, null, $"{label} không được là số âm.");
        }

        return (true, number, null);
    }

    private static (string First, string Last) SplitName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return ("Admin", string.Empty);
        }

        return parts.Length == 1
            ? (parts[0], string.Empty)
            : (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static string? ReadString(JsonElement root, params string[] path)
    {
        if (!TryReadPath(root, path, out var value))
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

    private static string? ReadAnyString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            var value = ReadString(root, name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static DateTime? ReadDateTime(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryReadPath(root, [name], out var value) &&
                value.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(value.GetString(), out var dateTime))
            {
                return dateTime.ToUniversalTime();
            }
        }

        return null;
    }

    private static bool TryReadPath(JsonElement element, IReadOnlyList<string> path, out JsonElement value)
    {
        value = element;
        foreach (var name in path)
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var found = false;
            foreach (var property in value.EnumerateObject())
            {
                if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = property.Value;
                found = true;
                break;
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private sealed record PackageNumberValues(
        bool Succeeded,
        decimal? LengthCm,
        decimal? WidthCm,
        decimal? HeightCm,
        decimal? DeclaredValue,
        string? Message)
    {
        public static PackageNumberValues Success(
            decimal? lengthCm,
            decimal? widthCm,
            decimal? heightCm,
            decimal? declaredValue) =>
            new(true, lengthCm, widthCm, heightCm, declaredValue, null);

        public static PackageNumberValues Failed(string message) =>
            new(false, null, null, null, null, message);
    }

    private sealed record AddressCodeResult(
        bool Succeeded,
        int? DistrictId,
        string? WardCode,
        string? Message)
    {
        public static AddressCodeResult Success(int? districtId, string? wardCode) =>
            new(true, districtId, wardCode, null);

        public static AddressCodeResult Failed(string message) =>
            new(false, null, null, message);
    }

    private sealed record RequestBuildResult<T>(
        bool Succeeded,
        T? Value,
        string? Message)
        where T : class
    {
        public static RequestBuildResult<T> Success(T value) =>
            new(true, value, null);

        public static RequestBuildResult<T> Failed(string message) =>
            new(false, null, message);
    }
}
