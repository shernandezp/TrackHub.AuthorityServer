﻿// Copyright (c) 2024 Sergio Hernandez. All rights reserved.
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

using OpenIddict.Abstractions;
using System.Security.Claims;

namespace Security.Web.Endpoints;

public sealed class LogoffHandler(IOpenIddictTokenManager tokenManager)
{
    /// <summary>
    /// Revokes all the tokens associated with the authenticated user.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>An asynchronous task that represents the token revocation operation</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task RevokeTokensAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.Sid)?.Value
                ?? throw new InvalidOperationException("User ID not found in the token.");

        await foreach (var token in tokenManager.FindBySubjectAsync(userId))
        {
            await tokenManager.TryRevokeAsync(token);
        }
    }
}