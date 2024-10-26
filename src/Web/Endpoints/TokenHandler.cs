// Copyright (c) 2024 Sergio Hernandez. All rights reserved.
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

using Microsoft.AspNetCore;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Abstractions;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Security.Web.Endpoints;

// Responsible for handling token exchange requests.
public sealed class TokenHandler()
{
    /// <summary>
    /// Handles the token exchange request.
    /// </summary>
    /// <param name="context">The HttpContext object representing the current HTTP request.</param>
    /// <returns>An asynchronous task that represents the token exchange operation.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<IResult> Exchange(HttpContext context)
    {
        // Retrieve the OpenID Connect request from the context
        var request = context.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Check if the grant type is supported
        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal stored in the authorization code
            var claimsPrincipal = (await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

            // Return the result based on the claims principal
            return claimsPrincipal != null
                ? Results.SignIn(claimsPrincipal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
                : throw new NotImplementedException("The specified grant type is not implemented.");
        }
        else if (request.IsClientCredentialsGrantType())
        {
            // Validate the client credentials
            var applicationManager = context.RequestServices.GetRequiredService<IOpenIddictApplicationManager>();
            if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret)) 
            {
                throw new InvalidOperationException("The client credentials are missing.");
            }

            var application = await applicationManager.FindByClientIdAsync(request.ClientId, context.RequestAborted);

            if (application == null || !await applicationManager.ValidateClientSecretAsync(application, request.ClientSecret, context.RequestAborted))
            {
                return Results.Unauthorized();
            }

            // Create a new claims principal
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.AddClaim(OpenIddictConstants.Claims.Subject, "service",
                OpenIddictConstants.Destinations.AccessToken);

            var principal = new ClaimsPrincipal(identity);

            // Set the scopes granted to the client application
            principal.SetScopes(request.GetScopes());

            // Return the result based on the claims principal
            return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        else 
        {
            throw new InvalidOperationException("The specified grant type is not supported.");
        }
    }
}
