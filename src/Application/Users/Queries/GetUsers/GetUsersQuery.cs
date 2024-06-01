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

using System.Security.Authentication;
using Security.Domain.Interfaces;
using Security.Domain.Models;
using Common.Domain.Extensions;

namespace Security.Application.Users.Queries.GetUsers;

public readonly record struct GetUsersQuery(string EmailAddress, string Password) : IRequest<UserVm>;

public class GetUsersQueryHandler(IUserReader reader) : IRequestHandler<GetUsersQuery, UserVm>
{
    public async Task<UserVm> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var user = await reader.GetUserAsync(new Domain.Records.UserLoginDto(request.EmailAddress, request.Password), cancellationToken);

        if (user.Verified == null)
            throw new AuthenticationException("User account hasn't been verified");

        if (!user.Active)
            throw new AuthenticationException("User account is inactive");

        if (user.Password.VerifyHashedPassword(request.Password))
        {
            user.Password = string.Empty;
            return user;
        }
        throw new AuthenticationException("Email or password is incorrect");
    }
}
