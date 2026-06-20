using e_commerce_web_admin.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class AccountController(
    SignInManager<Staff> signInManager,
    UserManager<Staff> userManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string? username, string? password, string? returnUrl = null)
    {
        var loginName = username?.Trim() ?? string.Empty;
        var inputPassword = password?.Trim() ?? string.Empty;

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Username"] = loginName;

        if (string.IsNullOrWhiteSpace(loginName))
        {
            ModelState.AddModelError("username", "Vui lòng nhập tên đăng nhập.");
        }

        if (string.IsNullOrWhiteSpace(inputPassword))
        {
            ModelState.AddModelError("password", "Vui lòng nhập mật khẩu.");
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        var staff = await userManager.FindByNameAsync(loginName)
            ?? await userManager.FindByEmailAsync(loginName);

        if (staff is null || !staff.IsActive)
        {
            ModelState.AddModelError("password", "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View();
        }

        var result = await signInManager.CheckPasswordSignInAsync(staff, inputPassword, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("password", "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View();
        }

        await signInManager.SignInAsync(staff, isPersistent: true);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Không có quyền truy cập";
        return View();
    }
}
