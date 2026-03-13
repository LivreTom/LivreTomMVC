using LivreTom.Models;

namespace LivreTom.Services;

public class AuthenticationStateService(IHttpContextAccessor httpContextAccessor)
{
    private const string ErrorCookieKey = "AuthError";
    private const string ModeCookieKey = "AuthMode";

    public string LoginError
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context?.Request.Cookies.TryGetValue(ErrorCookieKey, out var value) == true)
                return Uri.UnescapeDataString(value);

            return "";
        }
        set
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null || context.Response.HasStarted) return;

            if (string.IsNullOrEmpty(value))
                context.Response.Cookies.Delete(ErrorCookieKey);
            else
                context.Response.Cookies.Append(ErrorCookieKey, Uri.EscapeDataString(value), ShortLivedCookie());
        }
    }

    public AuthMode AuthMode
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context?.Request.Cookies.TryGetValue(ModeCookieKey, out var value) == true
                && Enum.TryParse<AuthMode>(value, out var mode))
                return mode;

            return Models.AuthMode.Login;
        }
        set
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null || context.Response.HasStarted) return;

            context.Response.Cookies.Append(ModeCookieKey, value.ToString(), ShortLivedCookie());
        }
    }

    public void ClearErrors()
    {
        var context = httpContextAccessor.HttpContext;
        if (context != null && !context.Response.HasStarted)
        {
            context.Response.Cookies.Delete(ErrorCookieKey);
            context.Response.Cookies.Delete(ModeCookieKey);
        }
    }

    private static CookieOptions ShortLivedCookie() => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddMinutes(5)
    };

    public bool HasError => !string.IsNullOrEmpty(LoginError);
}
