using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Bảng điều khiển";
        return View();
    }

    // ─── API Endpoints (Frontend calls these via fetch) ────────────────────

    /// <summary>
    /// Trả về KPI tổng quan: doanh thu, đơn hàng, khách hàng, sản phẩm
    /// </summary>
    [HttpGet("/api/dashboard/kpis")]
    public IActionResult GetKpis()
    {
        // TODO: Thay bằng truy vấn database thực tế
        var data = new
        {
            revenue = new
            {
                value    = 1_284_500_000L,
                change   = 12.4,
                trend    = "up",
                sparkline = new[] { 820, 940, 880, 1010, 1120, 1050, 1190, 1280 }
            },
            orders = new
            {
                value    = 3_842,
                change   = 8.1,
                trend    = "up",
                sparkline = new[] { 280, 320, 295, 360, 410, 390, 450, 480 }
            },
            customers = new
            {
                value    = 18_640,
                change   = 5.3,
                trend    = "up",
                sparkline = new[] { 1200, 1350, 1280, 1410, 1520, 1480, 1600, 1720 }
            },
            products = new
            {
                value    = 2_156,
                change   = -2.1,
                trend    = "down",
                sparkline = new[] { 2200, 2180, 2160, 2190, 2170, 2140, 2160, 2156 }
            }
        };

        return Ok(data);
    }

    /// <summary>
    /// Doanh thu theo từng tháng trong năm hiện tại và năm trước
    /// </summary>
    [HttpGet("/api/dashboard/revenue-chart")]
    public IActionResult GetRevenueChart()
    {
        // TODO: Thay bằng truy vấn database thực tế
        var data = new
        {
            labels = new[] { "T1","T2","T3","T4","T5","T6","T7","T8","T9","T10","T11","T12" },
            currentYear = new[] { 820, 950, 880, 1010, 1120, 1050, 1190, 1280, 1150, 1310, 1420, 1580 },
            previousYear = new[] { 640, 720, 680, 790, 860, 820, 910, 980, 890, 1020, 1100, 1240 }
        };

        return Ok(data);
    }

    /// <summary>
    /// Phân bố đơn hàng theo trạng thái
    /// </summary>
    [HttpGet("/api/dashboard/order-status")]
    public IActionResult GetOrderStatus()
    {
        // TODO: Thay bằng truy vấn database thực tế
        var data = new
        {
            labels = new[] { "Chờ xác nhận", "Đang xử lý", "Đang giao", "Đã giao", "Đã hủy" },
            values = new[] { 142, 380, 256, 2840, 224 },
            colors = new[] { "#f59e0b", "#3b82f6", "#8b5cf6", "#10b981", "#ef4444" }
        };

        return Ok(data);
    }

    /// <summary>
    /// Top 5 sản phẩm bán chạy nhất
    /// </summary>
    [HttpGet("/api/dashboard/top-products")]
    public IActionResult GetTopProducts()
    {
        // TODO: Thay bằng truy vấn database thực tế
        var data = new[]
        {
            new { rank=1, name="iPhone 15 Pro Max 256GB",  category="Điện thoại",   sold=482, revenue=5_784_000_000L, growth= 18.2 },
            new { rank=2, name="MacBook Air M3 13\"",       category="Laptop",       sold=218, revenue=3_926_400_000L, growth= 24.1 },
            new { rank=3, name="Samsung Galaxy S24 Ultra",  category="Điện thoại",   sold=376, revenue=3_385_600_000L, growth=  9.7 },
            new { rank=4, name="AirPods Pro 2nd Gen",       category="Phụ kiện",     sold=841, revenue=1_681_580_000L, growth= 31.5 },
            new { rank=5, name="iPad Pro M4 11\"",           category="Máy tính bảng",sold=195, revenue=2_984_700_000L, growth=-4.3  }
        };

        return Ok(data);
    }

    /// <summary>
    /// Đơn hàng gần đây nhất (10 đơn)
    /// </summary>
    [HttpGet("/api/dashboard/recent-orders")]
    public IActionResult GetRecentOrders()
    {
        // TODO: Thay bằng truy vấn database thực tế
        var data = new[]
        {
            new { id="#ORD-8821", customer="Nguyễn Văn An",    total=12_490_000L, status="Đang giao",    statusKey="shipping",  date="27/05/2026 21:42" },
            new { id="#ORD-8820", customer="Trần Thị Bình",   total= 2_990_000L, status="Đã giao",      statusKey="delivered", date="27/05/2026 20:15" },
            new { id="#ORD-8819", customer="Lê Minh Châu",    total=24_800_000L, status="Chờ xác nhận", statusKey="pending",   date="27/05/2026 19:30" },
            new { id="#ORD-8818", customer="Phạm Thị Dung",   total= 8_250_000L, status="Đang xử lý",   statusKey="processing",date="27/05/2026 18:05" },
            new { id="#ORD-8817", customer="Hoàng Văn Em",    total= 1_490_000L, status="Đã hủy",       statusKey="cancelled", date="27/05/2026 17:22" },
            new { id="#ORD-8816", customer="Vũ Thị Phương",   total=34_200_000L, status="Đã giao",      statusKey="delivered", date="27/05/2026 16:48" },
            new { id="#ORD-8815", customer="Đặng Minh Giang", total= 5_680_000L, status="Đang giao",    statusKey="shipping",  date="27/05/2026 15:10" },
            new { id="#ORD-8814", customer="Bùi Thị Hoa",     total=16_990_000L, status="Đang xử lý",   statusKey="processing",date="27/05/2026 14:33" },
        };

        return Ok(data);
    }

    /// <summary>
    /// Doanh thu theo danh mục sản phẩm
    /// </summary>
    [HttpGet("/api/dashboard/category-revenue")]
    public IActionResult GetCategoryRevenue()
    {
        // TODO: Thay bằng truy vấn database thực tế
        var data = new
        {
            labels = new[] { "Điện thoại", "Laptop", "Máy tính bảng", "Phụ kiện", "TV & Màn hình", "Khác" },
            values = new[] { 38.4, 24.2, 14.8, 11.6, 7.2, 3.8 }
        };

        return Ok(data);
    }

    /// <summary>
    /// Lượt truy cập website theo ngày (7 ngày gần nhất)
    /// </summary>
    [HttpGet("/api/dashboard/traffic")]
    public IActionResult GetTraffic()
    {
        // TODO: Thay bằng truy vấn database thực tế
        var today = DateTime.Today;
        var labels = Enumerable.Range(6, 0).Select(i => today.AddDays(-i).ToString("dd/MM")).Reverse().ToArray();
        var data = new
        {
            labels,
            sessions   = new[] { 4820, 5230, 4980, 6120, 5840, 7210, 8340 },
            pageViews  = new[] { 14200, 15800, 14100, 18400, 17200, 21600, 25100 },
            newUsers   = new[] { 1240, 1380, 1190, 1820, 1640, 2010, 2380 }
        };

        return Ok(data);
    }
}
