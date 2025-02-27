// Copyright (c) 2025 Sergio Hernandez. All rights reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License").
//  You may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Security.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Security.Application.Users.Queries.GetUsers;
using System.Security.Authentication;
using Microsoft.Extensions.Localization;

namespace Web.Controllers;

public class LoginController(ISender sender, IStringLocalizer<LoginController> localizer) : Controller
{
    // GET: /Login
    // Returns the login view.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Index(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    // POST: /Login
    // Handles the login form submission.
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            model.AuthenticationFailed = true;
            model.AuthenticationFailedMessage = localizer["CorrectErrors"];
            return View(model);
        }

        try
        {
            // Send a query to retrieve the user based on the provided email and password.
            var user = await sender.Send(new GetUsersQuery(model.Email, model.Password));

            // Create claims for the authenticated user.
            var claims = new List<Claim> { new(ClaimTypes.Sid, $"{user.UserId}") };

            // Create a claims identity using the claims and the default authentication scheme.
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Sign in the user with the created claims identity.
            await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));

            // Redirect the user to the return URL.
            return Redirect(model.ReturnUrl);
        }
        catch (AuthenticationException ex)
        {
            model.AuthenticationFailedMessage = localizer[ex.Message] ?? localizer["UnknownError"];
        }
        catch
        {
            model.AuthenticationFailedMessage = localizer["UnknownError"];
        }

        model.AuthenticationFailed = true;
        return View(model);
    }

}
