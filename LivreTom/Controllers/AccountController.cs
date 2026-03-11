using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LivreTom.Models;
using System.Security.Claims;

namespace LivreTom.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("login")]
    public ActionResult Login(string returnUrl = "/")
    {
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, Url.Action("GoogleCallback", new { returnUrl }));
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("GoogleCallback")]
    public async Task<IActionResult> GoogleCallback(string returnUrl = "/", string remoteError = null)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null) return RedirectToAction("Login");

        // Tenta logar o usuário se ele já existir
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true);

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }

        // Se não existe, vamos criar o usuário agora
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var user = new ApplicationUser { UserName = email, Email = email, Credits = 1 }; // 1 Crédito inicial aqui!

        var createResult = await _userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, isPersistent: true);
        }

        return LocalRedirect(returnUrl);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/");
    }
}