using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceHub.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public IActionResult SignIn(string? returnUrl = null)
        {
            var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? Url.Action("Index", "Home")! : returnUrl;
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [Authorize]
        public IActionResult SignOutCurrentUser()
        {
            var redirectUri = Url.Action("Index", "Home");
            return SignOut(
                new AuthenticationProperties { RedirectUri = redirectUri },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
