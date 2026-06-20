using System.Diagnostics;
using e_commerce_web_admin.Filters;
using Microsoft.AspNetCore.Mvc;
using e_commerce_web_admin.Models;
using e_commerce_web_admin.Models.Constants;

namespace e_commerce_web_admin.Controllers;

public class HomeController : Controller
{
    [RbacAuthorize("Dashboard", Permissions.View)]
    public IActionResult Index()
    {
        return View();
    }

    [RbacAuthorize("Dashboard", Permissions.View)]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
