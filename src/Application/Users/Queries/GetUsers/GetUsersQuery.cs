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
using Security.Application.Users.Events;

namespace Security.Application.Users.Queries.GetUsers;

public readonly record struct GetUsersQuery(string EmailAddress, string Password) : IRequest<UserVm>;

// Handles the GetUsersQuery and returns a UserVm
public class GetUsersQueryHandler(IUserReader reader, IPublisher publisher) : IRequestHandler<GetUsersQuery, UserVm>
{
    // Handles the GetUsersQuery and returns a UserVm
    public async Task<UserVm> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var user = await reader.GetUserAsync(new Domain.Records.UserLoginDto(request.EmailAddress, request.Password), cancellationToken);

        if (user == default)
            throw new AuthenticationException("Email is incorrect");

        if (user.Verified == null)
            throw new AuthenticationException("User account hasn't been verified");

        if (!user.Active)
            throw new AuthenticationException("User account is inactive");

        if (user.LoginAttempts > 3)
            throw new AuthenticationException("Too many failed login attempts");

        if (user.Password.VerifyHashedPassword(request.Password))
        {
            user.Password = string.Empty;
            return user;
        }
        else 
        {
            await publisher.Publish(new LoginAttempt.Notification(user.UserId), cancellationToken);
            throw new AuthenticationException("Password is incorrect");
        }

        throw new AuthenticationException("Email or password is incorrect");
    }
}
