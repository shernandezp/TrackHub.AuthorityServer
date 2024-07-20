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

namespace Security.Web.Endpoints;

// Responsible for handling token exchange requests.
public sealed class TokenHandler()
{
    // Handles the token exchange request.
    // Parameters:
    // - context: The HttpContext object representing the current HTTP request.
    // Returns:
    // - An asynchronous task that represents the token exchange operation.
    public async Task<IResult> Exchange(HttpContext context)
    {
        // Retrieve the OpenID Connect request from the context
        var request = context.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Check if the grant type is supported
        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            throw new InvalidOperationException("The specified grant type is not supported.");

        // Retrieve the claims principal stored in the authorization code
        var claimsPrincipal = (await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

        // Return the result based on the claims principal
        return claimsPrincipal != null
            ? Results.SignIn(claimsPrincipal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
            : throw new NotImplementedException("The specified grant type is not implemented.");
    }
}
