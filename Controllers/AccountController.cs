using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Neo4j.Driver;
using Nosql_Neo4j.Models;
using Nosql_Neo4j.Services;
namespace Nosql_Neo4j.Controllers;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class AccountController(AccountService accounts, ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl)
        => View(new LoginInput { ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null });

    [AllowAnonymous, HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        input.ReturnUrl = Url.IsLocalUrl(input.ReturnUrl) ? input.ReturnUrl : null;
        if (!ModelState.IsValid) return View(input);
        try
        {
            var user = await accounts.AuthenticateAsync(input.Username, input.Password);
            if (user is null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                input.Password = "";
                ModelState.Remove(nameof(input.Password));
                return View(input);
            }
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role), new Claim("security_stamp", user.SecurityStamp)
            }, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false });
            return input.ReturnUrl is not null ? LocalRedirect(input.ReturnUrl) : RedirectToAction("Index", "Practice");
        }
        catch (Neo4jException exception)
        {
            logger.LogError("Không xác thực được tài khoản; loại lỗi {Type}; request {RequestId}",
                exception.GetType().Name, HttpContext.TraceIdentifier);
            Response.StatusCode = 503;
            ModelState.AddModelError("", "Dịch vụ dữ liệu tạm thời không sẵn sàng. Hãy thử lại.");
            input.Password = "";
            ModelState.Remove(nameof(input.Password));
            return View(input);
        }
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordInput());

    [Authorize, HttpPost, EnableRateLimiting("login")]
    public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
    {
        try
        {
            if (ModelState.IsValid && await accounts.ChangePasswordAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!, input.CurrentPassword, input.NewPassword))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["AccountMessage"] = "Đã đổi mật khẩu và kết thúc các phiên đăng nhập cũ. Hãy đăng nhập lại.";
                return RedirectToAction(nameof(Login));
            }
            if (ModelState.IsValid) ModelState.AddModelError("", "Mật khẩu hiện tại chưa đúng, mật khẩu mới trùng mật khẩu cũ hoặc tài khoản vừa thay đổi.");
        }
        catch (Neo4jException e)
        {
            logger.LogError("Không đổi được mật khẩu; loại lỗi {Type}; request {RequestId}", e.GetType().Name, HttpContext.TraceIdentifier);
            Response.StatusCode = 503;
            ModelState.AddModelError("", "Dịch vụ dữ liệu tạm thời không sẵn sàng. Hãy thử lại.");
        }
        foreach (var key in new[] { nameof(input.CurrentPassword), nameof(input.NewPassword), nameof(input.ConfirmPassword) })
        {
            if (ModelState.TryGetValue(key, out var entry)) { entry.RawValue = null; entry.AttemptedValue = null; }
        }
        return View(new ChangePasswordInput());
    }

    [HttpGet]
    public IActionResult AccessDenied() => StatusCode(403, "Bạn không có quyền thực hiện thao tác này.");
}
