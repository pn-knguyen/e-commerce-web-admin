using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Promotions", Permissions.View)]
public sealed class CampaignsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
