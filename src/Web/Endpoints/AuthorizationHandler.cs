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
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Microsoft.AspNetCore;

namespace Web.Endpoints;

// This class handles the authorization process.
public sealed class AuthorizationHandler
{
    // This method is responsible for authorizing the request.
    public async Task Authorize(HttpContext context)
    {
        // Retrieve the OpenID Connect request from the context.
        var request = context.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Retrieve the user principal stored in the authentication cookie.
        var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // If the user principal can't be extracted, challenge the authentication scheme.
        if (!result.Succeeded)
        {
            await context.ChallengeAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = context.Request.PathBase + context.Request.Path + QueryString.Create(
                    context.Request.HasFormContentType ? [.. context.Request.Form] : context.Request.Query.ToList())
                });
            return;
        }

        // Create a list of claims for the user.
        var claims = new List<Claim>
            {
                new(OpenIddictConstants.Claims.Subject, result.Principal?.Claims.Single(x => x.Type == ClaimTypes.Sid).Value ?? string.Empty)
            };

        // Create a claims identity and principal.
        var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Set the scopes for the claims principal.
        claimsPrincipal.SetScopes(request.GetScopes());

        // Sign in the claims principal.
        if (claimsPrincipal != null)
        {
            await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
        }
    }
}
