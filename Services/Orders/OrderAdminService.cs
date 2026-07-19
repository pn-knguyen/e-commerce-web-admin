using System.Data;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Services.Inventory;
using e_commerce_web_admin.Services.Shipping;
using e_commerce_web_admin.ViewModels.Orders;
using e_commerce_web_admin.ViewModels.Shipments;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Orders;

public sealed class OrderAdminService : IOrderAdminService
{
    private const int DefaultPageSize = 20;

    private readonly ApplicationDbContext _db;
    private readonly IShipmentAdminService _shipmentService;
    private readonly IInventoryLedgerService _inventoryLedger;

    private sealed class OrderIndexSummary
    {
        public int PendingCount { get; init; }
        public int ShippingCount { get; init; }
        public int CompletedCount { get; init; }
        public decimal CompletedRevenue { get; init; }
    }

    private sealed record DateRangeFilter(DateTime? StartUtc, DateTime? EndUtc);

    public OrderAdminService(
        ApplicationDbContext db,
        IShipmentAdminService shipmentService,
        IInventoryLedgerService inventoryLedger)
    {
        _db = db;
        _shipmentService = shipmentService;
        _inventoryLedger = inventoryLedger;
    }

    public async Task<OrderIndexViewModel> GetIndexAsync(
        OrderIndexQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var dbQuery = ApplyFilters(_db.Orders.AsNoTracking(), query);

        var totalCount = await dbQuery.CountAsync(ct);
        var summary = await dbQuery
            .GroupBy(_ => 1)
            .Select(group => new OrderIndexSummary
            {
                PendingCount = group.Count(order => order.OrderStatus == OrderStatus.Pending),
                ShippingCount = group.Count(order => order.OrderStatus == OrderStatus.Shipping),
                CompletedCount = group.Count(order => order.OrderStatus == OrderStatus.Completed),
                CompletedRevenue = group
                    .Where(order => order.OrderStatus == OrderStatus.Completed &&
                        order.PaymentStatus == PaymentStatus.Paid)
                    .Sum(order => (decimal?)order.TotalAmount) ?? 0m,
            })
            .FirstOrDefaultAsync(ct) ?? new OrderIndexSummary();
        var rows = await dbQuery
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(order => new OrderRowViewModel
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                CustomerName = order.User != null ? order.User.FullName : order.ShippingContactName,
                CustomerEmail = order.User != null ? order.User.Email : null,
                ShippingPhone = order.ShippingPhone,
                PaymentMethodName = order.PaymentMethod != null ? order.PaymentMethod.Name : "Không rõ",
                ItemCount = order.OrderItems.Sum(item => item.Quantity),
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = order.CreatedAt,
            })
            .ToListAsync(ct);

        return new OrderIndexViewModel
        {
            Orders = rows,
            Search = query.Search,
            DateRange = NormalizeDateRange(query),
            CreatedFrom = query.CreatedFrom,
            CreatedTo = query.CreatedTo,
            OrderStatus = query.OrderStatus,
            PaymentStatus = query.PaymentStatus,
            PaymentMethodId = query.PaymentMethodId,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            PendingCount = summary.PendingCount,
            ShippingCount = summary.ShippingCount,
            CompletedCount = summary.CompletedCount,
            CompletedRevenue = summary.CompletedRevenue,
            DateRangeOptions = BuildDateRangeOptions(query),
            OrderStatusOptions = BuildOrderStatusOptions(query.OrderStatus),
            PaymentStatusOptions = BuildPaymentStatusOptions(query.PaymentStatus),
            PaymentMethodOptions = await BuildPaymentMethodOptionsAsync(query.PaymentMethodId, ct),
        };
    }

    public async Task<OrderDetailsViewModel?> GetDetailsAsync(long id, CancellationToken ct = default)
    {
        var viewModel = await _db.Orders
            .AsNoTracking()
            .Where(order => order.Id == id)
            .Select(order => new OrderDetailsViewModel
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                CustomerName = order.User != null ? order.User.FullName : order.ShippingContactName,
                CustomerEmail = order.User != null ? order.User.Email : null,
                CustomerPhone = order.User != null ? order.User.Phone : null,
                PaymentMethodId = order.PaymentMethodId,
                PaymentMethodName = order.PaymentMethod != null ? order.PaymentMethod.Name : "Không rõ",
                IsCashOnDelivery = order.PaymentMethodId == PaymentMethodIds.CashOnDelivery ||
                    (order.PaymentMethod != null &&
                        (order.PaymentMethod.Name.Contains("COD") ||
                            order.PaymentMethod.Name.Contains("nhận hàng"))),
                VoucherCode = order.Voucher != null ? order.Voucher.Code : null,
                ShippingContactName = order.ShippingContactName,
                ShippingPhone = order.ShippingPhone,
                ShippingProvince = order.ShippingProvince,
                ShippingWard = order.ShippingWard,
                ShippingDetail = order.ShippingDetail,
                SubtotalAmount = order.SubtotalAmount,
                ShippingFee = order.ShippingFee,
                VoucherDiscount = order.VoucherDiscount,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.OrderItems
                    .OrderBy(item => item.Id)
                    .Select(item => new OrderItemViewModel
                    {
                        Id = item.Id,
                        ProductName = item.ProductVariant != null && item.ProductVariant.Product != null
                            ? item.ProductVariant.Product.Name
                            : "Sản phẩm không xác định",
                        VariantCode = item.ProductVariant != null ? item.ProductVariant.Code : "N/A",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        UnitCost = item.UnitCost,
                        LineTotal = item.UnitPrice * item.Quantity,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (viewModel is null)
        {
            return null;
        }

        viewModel.OrderStatusOptions = BuildOrderStatusOptions(viewModel.OrderStatus.ToString());
        viewModel.PaymentStatusOptions = BuildPaymentStatusOptions(viewModel.PaymentStatus.ToString());
        viewModel.StatusForm = new OrderStatusUpdateViewModel
        {
            Id = viewModel.Id,
            OrderStatus = viewModel.OrderStatus,
            PaymentStatus = viewModel.PaymentStatus,
        };
        viewModel.ShipmentPanel = await _shipmentService.GetPanelAsync(viewModel.Id, ct);

        return viewModel;
    }

    public async Task<OrderStatusUpdateResult> UpdateStatusAsync(
        long id,
        OrderStatusUpdateViewModel form,
        CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.RepeatableRead, ct);

            var order = await _db.Orders
                .Include(o => o.Shipments)
                .Include(o => o.OrderItems)
                    .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant!.Product)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order is null)
            {
                return OrderStatusUpdateResult.NotFound();
            }

            var latestShipment = order.Shipments
                .OrderByDescending(s => s.CreatedAt)
                .ThenByDescending(s => s.Id)
                .FirstOrDefault();

            var errors = ValidateStatusChange(order, form, latestShipment);
            if (errors.Count > 0)
            {
                return OrderStatusUpdateResult.Failed(errors);
            }

            if (form.OrderStatus == OrderStatus.Completed &&
                IsCashOnDelivery(order) &&
                form.PaymentStatus != PaymentStatus.Refunded)
            {
                form.PaymentStatus = PaymentStatus.Paid;
            }

            if (order.OrderStatus == form.OrderStatus && order.PaymentStatus == form.PaymentStatus)
            {
                return OrderStatusUpdateResult.Success("Đơn hàng chưa có thay đổi trạng thái.");
            }

            var previousOrderStatus = order.OrderStatus;
            order.OrderStatus = form.OrderStatus;
            order.PaymentStatus = form.PaymentStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (form.OrderStatus == OrderStatus.Completed)
            {
                await _inventoryLedger.ApplyOrderSaleAsync(order, latestShipment?.FulfillmentLocationId, ct);
            }

            await _inventoryLedger.ApplyOrderStatusChangeAsync(order, previousOrderStatus, form.OrderStatus, ct);

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return OrderStatusUpdateResult.Success(
                $"Đã cập nhật đơn hàng {order.OrderCode} sang {OrderDisplay.GetOrderStatusLabel(order.OrderStatus).ToLowerInvariant()}.");
        });
    }

    private static IQueryable<Order> ApplyFilters(IQueryable<Order> query, OrderIndexQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            query = query.Where(order =>
                order.OrderCode.Contains(term) ||
                order.ShippingContactName.Contains(term) ||
                order.ShippingPhone.Contains(term) ||
                    (order.User != null &&
                    (order.User.FullName.Contains(term) || order.User.Email.Contains(term))));
        }

        var dateRange = GetDateRange(filters);
        if (dateRange is not null)
        {
            if (dateRange.StartUtc.HasValue)
            {
                query = query.Where(order => order.CreatedAt >= dateRange.StartUtc.Value);
            }

            if (dateRange.EndUtc.HasValue)
            {
                query = query.Where(order => order.CreatedAt < dateRange.EndUtc.Value);
            }
        }

        if (TryParseOrderStatus(filters.OrderStatus, out var orderStatus))
        {
            query = query.Where(order => order.OrderStatus == orderStatus);
        }

        if (TryParsePaymentStatus(filters.PaymentStatus, out var paymentStatus))
        {
            query = query.Where(order => order.PaymentStatus == paymentStatus);
        }

        if (filters.PaymentMethodId is > 0)
        {
            query = query.Where(order => order.PaymentMethodId == filters.PaymentMethodId.Value);
        }

        return query;
    }

    private static List<OrderValidationError> ValidateStatusChange(
        Order order,
        OrderStatusUpdateViewModel form,
        Shipment? latestShipment)
    {
        var errors = new List<OrderValidationError>();

        if (!CanChangeOrderStatus(order.OrderStatus, form.OrderStatus))
        {
            errors.Add(new OrderValidationError(
                nameof(form.OrderStatus),
                $"Không thể chuyển đơn từ \"{OrderDisplay.GetOrderStatusLabel(order.OrderStatus)}\" sang \"{OrderDisplay.GetOrderStatusLabel(form.OrderStatus)}\"."));
        }

        if (!CanChangePaymentStatus(order.PaymentStatus, form.PaymentStatus))
        {
            errors.Add(new OrderValidationError(
                nameof(form.PaymentStatus),
                $"Không thể chuyển thanh toán từ \"{OrderDisplay.GetPaymentStatusLabel(order.PaymentStatus)}\" sang \"{OrderDisplay.GetPaymentStatusLabel(form.PaymentStatus)}\"."));
        }

        if (form.OrderStatus == OrderStatus.Completed &&
            latestShipment is not null &&
            latestShipment.Status != ShipmentStatus.Delivered)
        {
            errors.Add(new OrderValidationError(
                nameof(form.OrderStatus),
                $"Chưa thể chuyển đơn sang đã giao vì vận đơn đang ở trạng thái \"{ShipmentDisplay.GetStatusLabel(latestShipment.Status)}\"."));
        }

        if (form.PaymentStatus == PaymentStatus.Refunded &&
            form.OrderStatus is not OrderStatus.Cancelled and not OrderStatus.Returned)
        {
            errors.Add(new OrderValidationError(
                nameof(form.PaymentStatus),
                "Chỉ hoàn tiền cho đơn đã hủy hoặc đã trả hàng."));
        }

        // Ngăn admin gán Paid cho đơn bị hủy / trả trong trường hợp đơn chưa được thanh toán.
        // (Khi đơn đã Paid rồi, Rule phía dưới sẽ xử lý riêng — tránh hiển thị lỗi trùng.)
        if (form.OrderStatus is OrderStatus.Cancelled or OrderStatus.Returned &&
            form.PaymentStatus == PaymentStatus.Paid &&
            order.PaymentStatus != PaymentStatus.Paid)
        {
            errors.Add(new OrderValidationError(
                nameof(form.PaymentStatus),
                "Đơn đã hủy hoặc trả hàng không thể chuyển sang trạng thái đã thanh toán."));
        }

        // Đơn đã thanh toán khi bị hủy / trả bắt buộc phải hoàn tiền.
        if (form.OrderStatus is OrderStatus.Cancelled or OrderStatus.Returned &&
            order.PaymentStatus == PaymentStatus.Paid &&
            form.PaymentStatus != PaymentStatus.Refunded)
        {
            errors.Add(new OrderValidationError(
                nameof(form.PaymentStatus),
                "Đơn đã thanh toán khi hủy hoặc trả hàng phải chuyển sang đã hoàn tiền."));
        }

        return errors;
    }

    private static bool IsCashOnDelivery(Order order) =>
        order.PaymentMethodId == PaymentMethodIds.CashOnDelivery;

    private static bool CanChangeOrderStatus(OrderStatus current, OrderStatus next) =>
        current switch
        {
            OrderStatus.Pending => next is OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => next is OrderStatus.Confirmed or OrderStatus.Processing or OrderStatus.Cancelled,
            OrderStatus.Processing => next is OrderStatus.Processing or OrderStatus.Shipping or OrderStatus.Cancelled,
            OrderStatus.Shipping => next is OrderStatus.Shipping or OrderStatus.Completed or OrderStatus.Returned,
            OrderStatus.Completed => next is OrderStatus.Completed or OrderStatus.Returned,
            OrderStatus.Cancelled => next is OrderStatus.Cancelled,
            OrderStatus.Returned => next is OrderStatus.Returned,
            _ => false,
        };

    private static bool CanChangePaymentStatus(PaymentStatus current, PaymentStatus next) =>
        current switch
        {
            PaymentStatus.Unpaid => next is PaymentStatus.Unpaid or PaymentStatus.Paid or PaymentStatus.Failed,
            PaymentStatus.Failed => next is PaymentStatus.Failed or PaymentStatus.Unpaid or PaymentStatus.Paid,
            PaymentStatus.Paid => next is PaymentStatus.Paid or PaymentStatus.Refunded,
            PaymentStatus.Refunded => next is PaymentStatus.Refunded,
            _ => false,
        };

    private static bool TryParseOrderStatus(string? value, out OrderStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    private static bool TryParsePaymentStatus(string? value, out PaymentStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    private static List<OrderFilterOption> BuildDateRangeOptions(OrderIndexQuery query)
    {
        var selected = NormalizeDateRange(query);
        return
        [
            new OrderFilterOption
            {
                Value = "today",
                Text = "Hôm nay",
                Selected = selected == "today",
            },
            new OrderFilterOption
            {
                Value = "yesterday",
                Text = "Hôm qua",
                Selected = selected == "yesterday",
            },
            new OrderFilterOption
            {
                Value = "last7days",
                Text = "7 ngày gần nhất",
                Selected = selected == "last7days",
            },
            new OrderFilterOption
            {
                Value = "last30days",
                Text = "30 ngày gần nhất",
                Selected = selected == "last30days",
            },
            new OrderFilterOption
            {
                Value = "thismonth",
                Text = "Tháng này",
                Selected = selected == "thismonth",
            },
            new OrderFilterOption
            {
                Value = "lastmonth",
                Text = "Tháng trước",
                Selected = selected == "lastmonth",
            },
            new OrderFilterOption
            {
                Value = "thisquarter",
                Text = "Quý này",
                Selected = selected == "thisquarter",
            },
            new OrderFilterOption
            {
                Value = "thisyear",
                Text = "Năm nay",
                Selected = selected == "thisyear",
            },
            new OrderFilterOption
            {
                Value = "custom",
                Text = "Tùy chọn khoảng thời gian",
                Selected = selected == "custom",
            },
        ];
    }

    private static List<OrderFilterOption> BuildOrderStatusOptions(string? selectedValue) =>
        Enum.GetValues<OrderStatus>()
            .Select(status => new OrderFilterOption
            {
                Value = status.ToString(),
                Text = OrderDisplay.GetOrderStatusLabel(status),
                Selected = string.Equals(selectedValue, status.ToString(), StringComparison.OrdinalIgnoreCase),
            })
            .ToList();

    private static List<OrderFilterOption> BuildPaymentStatusOptions(string? selectedValue) =>
        Enum.GetValues<PaymentStatus>()
            .Select(status => new OrderFilterOption
            {
                Value = status.ToString(),
                Text = OrderDisplay.GetPaymentStatusLabel(status),
                Selected = string.Equals(selectedValue, status.ToString(), StringComparison.OrdinalIgnoreCase),
            })
            .ToList();

    private static DateRangeFilter? GetDateRange(OrderIndexQuery query)
    {
        var normalizedValue = NormalizeDateRange(query);
        if (normalizedValue is null)
        {
            return null;
        }

        var timeZone = TimeZoneHelper.GetVietnamTimeZone();
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        if (normalizedValue == "custom")
        {
            return GetCustomDateRange(query, timeZone);
        }

        var localStart = normalizedValue switch
        {
            "yesterday" => today.AddDays(-1),
            "last7days" => today.AddDays(-6),
            "last30days" => today.AddDays(-29),
            "thismonth" => thisMonthStart,
            "lastmonth" => thisMonthStart.AddMonths(-1),
            "thisquarter" => GetQuarterStart(today),
            "thisyear" => new DateTime(today.Year, 1, 1),
            _ => today,
        };
        var localEnd = normalizedValue switch
        {
            "yesterday" => today,
            "lastmonth" => thisMonthStart,
            _ => today.AddDays(1),
        };

        return new DateRangeFilter(
            ToUtc(localStart, timeZone),
            ToUtc(localEnd, timeZone));
    }

    private static DateRangeFilter? GetCustomDateRange(OrderIndexQuery query, TimeZoneInfo timeZone)
    {
        var localStart = query.CreatedFrom;
        var localEnd = query.CreatedTo;

        if (!localStart.HasValue && !localEnd.HasValue)
        {
            return null;
        }

        if (localStart.HasValue && localEnd.HasValue && localEnd.Value < localStart.Value)
        {
            (localStart, localEnd) = (localEnd, localStart);
        }

        return new DateRangeFilter(
            localStart.HasValue ? ToUtc(localStart.Value, timeZone) : null,
            localEnd.HasValue ? ToUtc(localEnd.Value.AddMinutes(1), timeZone) : null);
    }

    private static DateTime ToUtc(DateTime localDateTime, TimeZoneInfo timeZone)
        => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified),
            timeZone);

    private static DateTime GetQuarterStart(DateTime date)
    {
        var quarterStartMonth = ((date.Month - 1) / 3 * 3) + 1;
        return new DateTime(date.Year, quarterStartMonth, 1);
    }

    private static string? NormalizeDateRange(OrderIndexQuery query)
    {
        if (query.CreatedFrom.HasValue || query.CreatedTo.HasValue)
        {
            return "custom";
        }

        if (string.IsNullOrWhiteSpace(query.DateRange))
        {
            return null;
        }

        return query.DateRange.Trim().ToLowerInvariant() switch
        {
            "today" => "today",
            "yesterday" => "yesterday",
            "last7days" => "last7days",
            "last30days" => "last30days",
            "thismonth" => "thismonth",
            "lastmonth" => "lastmonth",
            "thisquarter" => "thisquarter",
            "thisyear" => "thisyear",
            "custom" => "custom",
            _ => null,
        };
    }

    private async Task<List<OrderFilterOption>> BuildPaymentMethodOptionsAsync(
        long? selectedId,
        CancellationToken ct)
    {
        return await _db.PaymentMethods
            .AsNoTracking()
            .OrderBy(method => method.Name)
            .Select(method => new OrderFilterOption
            {
                Value = method.Id.ToString(),
                Text = method.Name,
                Selected = selectedId.HasValue && method.Id == selectedId.Value,
            })
            .ToListAsync(ct);
    }
}
