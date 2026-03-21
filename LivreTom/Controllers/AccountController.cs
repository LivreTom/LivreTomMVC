using LivreTom.Models;
using LivreTom.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LivreTom.Controllers;

[Route("[controller]")]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    AuthenticationStateService authService) : Controller
{
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password)
    {
        var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);

        if (result.Succeeded)
            return Redirect("/");

        return RedirectWithError("Usuário ou senha inválidos", AuthMode.Login);
    }

    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var properties = signInManager.ConfigureExternalAuthenticationProperties(
            "Google", Url.Action("GoogleCallback", "Account"));

        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return RedirectWithError("Falha ao autenticar com o Google", AuthMode.Login);

        var result = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: true);

        if (result.Succeeded)
            return Redirect("/");

        var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email)!;
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            await userManager.AddLoginAsync(existingUser, info);
            await signInManager.SignInAsync(existingUser, isPersistent: true);

            return Redirect("/");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Credits = 1,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            await userManager.AddLoginAsync(user, info);
            await signInManager.SignInAsync(user, isPersistent: true);

            return Redirect("/");
        }

        return RedirectWithError("Erro ao criar conta com Google", AuthMode.Login);
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromForm] string email, [FromForm] string password, [FromForm] string confirmPassword)
    {
        if (password != confirmPassword)
            return RedirectWithError("As senhas não coincidem", AuthMode.Register);

        if (await userManager.FindByEmailAsync(email) != null)
            return RedirectWithError("Este e-mail já está registrado", AuthMode.Register);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Credits = 1,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await signInManager.SignInAsync(user, isPersistent: true);

            return Redirect("/");
        }

        var errorDescription = result.Errors.FirstOrDefault()?.Description ?? "Erro ao criar conta";

        return RedirectWithError(errorDescription, AuthMode.Register);
    }

    [HttpPost("forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword([FromForm] string email, [FromServices] EmailService emailService)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { email, token }, Request.Scheme);
            await emailService.SendPasswordResetAsync(email, resetLink!);
        }

        authService.LoginError = "Se este e-mail estiver cadastrado, você receberá as instruções em breve.|Verifique sua caixa de entrada e spam.";
        authService.AuthMode = AuthMode.ForgotPassword;

        return Redirect("/");
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPassword([FromQuery] string email, [FromQuery] string token)
    {
        authService.LoginError = "";
        authService.AuthMode = AuthMode.ResetPassword;

        Response.Cookies.Append("ResetEmail", email, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });
        Response.Cookies.Append("ResetToken", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Redirect("/");
    }

    [HttpPost("reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordConfirm([FromForm] string password, [FromForm] string confirmPassword)
    {
        if (password != confirmPassword)
            return RedirectWithError("As senhas não coincidem", AuthMode.ResetPassword);

        var email = Request.Cookies["ResetEmail"];
        var token = Request.Cookies["ResetToken"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectWithError("Link expirado. Solicite um novo.", AuthMode.ForgotPassword);

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return RedirectWithError("Link expirado. Solicite um novo.", AuthMode.ForgotPassword);

        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (result.Succeeded)
        {
            Response.Cookies.Delete("ResetEmail");
            Response.Cookies.Delete("ResetToken");
            authService.LoginError = "Senha redefinida com sucesso! Faça login.";
            authService.AuthMode = AuthMode.Login;

            return Redirect("/");
        }

        var error = result.Errors.FirstOrDefault()?.Description ?? "Erro ao redefinir senha";
        return RedirectWithError(error, AuthMode.ResetPassword);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        authService.ClearErrors();

        return Redirect("/");
    }

    private RedirectResult RedirectWithError(string message, AuthMode mode)
    {
        authService.LoginError = message;
        authService.AuthMode = mode;

        return Redirect("/");
    }
}
