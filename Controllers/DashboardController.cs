using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Bảng điều khiển";
        return View();
    }
}
