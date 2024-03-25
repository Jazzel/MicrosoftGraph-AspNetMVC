// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using graph_tutorial.TokenStorage;
using graph_tutorial.Models;
using System;
using System.Linq;

namespace graph_tutorial.Controllers
{
    public class AccountController : Controller
    {
        ApplicationDbContext db;

        public AccountController()
        {
            db = new ApplicationDbContext();
        }
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                // Signal OWIN to send an authorization request to Azure
                Request.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);

                HttpCookie userIdCookie = new HttpCookie("logUpdated");
                userIdCookie.Value = "false";
                Response.Cookies.Add(userIdCookie);
            }
        }

        public ActionResult SignOut()
        {
            if (Request.IsAuthenticated)
            {
                var userClaims = User.Identity as ClaimsIdentity;

                var name = userClaims?.FindFirst("name")?.Value;

                var user = db.Users.Where(m => m.Name == name).FirstOrDefault();

                if (user != null)
                {
                    Log log = new Log()
                    {
                        Action = "Logout",
                        ApplicationUserId = user.Id,
                        Timestamp = DateTime.Now,
                    };

                    db.Logs.Add(log);
                    db.SaveChanges();
                }
                var tokenStore = new SessionTokenStore(null,
                    System.Web.HttpContext.Current, ClaimsPrincipal.Current);

                tokenStore.Clear();

                Request.GetOwinContext().Authentication.SignOut(
                    CookieAuthenticationDefaults.AuthenticationType);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}