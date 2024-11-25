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

using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Security.Infrastructure;

namespace ClientSeeder;

internal class Seeder(IServiceProvider serviceProvider)
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var databaseContext = scope.ServiceProvider.GetRequiredService<AuthorityDbContext>();
        databaseContext.Database.EnsureCreated();

        var clients = Clients.LoadFromFile("clients.json");

        await PopulateScopes(scope, clients.Scopes, cancellationToken);
        await PopulateInternalApps(scope, clients.PKCEClients, clients.ServiceClients, cancellationToken);
    }

    private static async ValueTask PopulateScopes(IServiceScope scope, Scope[] scopes, CancellationToken cancellationToken)
    {
        foreach (var scopeItem in scopes)
        {
            await PopulateScope(scope, scopeItem.Name, scopeItem.Resource, cancellationToken);
        }
    }

    private static async ValueTask PopulateScope(IServiceScope scope,
        string scopeName,
        string resource,
        CancellationToken cancellationToken)
    {
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        var scopeDescriptor = new OpenIddictScopeDescriptor
        {
            Name = scopeName,
            Resources = { resource }
        };

        var scopeInstance = await scopeManager.FindByNameAsync(scopeDescriptor.Name, cancellationToken);

        if (scopeInstance == null)
        {
            await scopeManager.CreateAsync(scopeDescriptor, cancellationToken);
        }
        else
        {
            await scopeManager.UpdateAsync(scopeInstance, scopeDescriptor, cancellationToken);
        }
    }

    private static async ValueTask PopulateInternalApps(IServiceScope scopeService, PKCEClient[] pkceClients, ServiceClient[] serviceClients, CancellationToken cancellationToken)
    {
        foreach (var pkceClient in pkceClients)
        {
            await PopulatePKCEApp(scopeService, pkceClient.ClientId, pkceClient.Uri, pkceClient.Scope, cancellationToken);
        }

        foreach (var serviceClient in serviceClients)
        {
            await PopulateInternalApp(scopeService, serviceClient.ClientId, serviceClient.ClientSecret, cancellationToken);
        }
    }

    private static async ValueTask PopulatePKCEApp(IServiceScope scopeService,
        string clientId,
        string uri,
        string scope,
        CancellationToken cancellationToken)
    {
        var appManager = scopeService.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var appDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
            RedirectUris = { new Uri(uri) },
            Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Authorization,
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.Endpoints.Introspection,
                        OpenIddictConstants.Permissions.Endpoints.Revocation,

                        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                        OpenIddictConstants.Permissions.GrantTypes.RefreshToken,

                        OpenIddictConstants.Permissions.Prefixes.Scope + scope,
                        OpenIddictConstants.Permissions.ResponseTypes.Code
                    }
        };

        var client = await appManager.FindByClientIdAsync(appDescriptor.ClientId, cancellationToken);

        if (client == null)
        {
            await appManager.CreateAsync(appDescriptor, cancellationToken);
        }
        else
        {
            await appManager.UpdateAsync(client, appDescriptor, cancellationToken);
        }
    }

    private static async ValueTask PopulateInternalApp(IServiceScope scopeService,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var appManager = scopeService.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var appDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ApplicationType = OpenIddictConstants.ApplicationTypes.Native,
            Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
                    }
        };

        var client = await appManager.FindByClientIdAsync(appDescriptor.ClientId, cancellationToken);

        if (client == null)
        {
            await appManager.CreateAsync(appDescriptor, cancellationToken);
        }
        else
        {
            await appManager.UpdateAsync(client, appDescriptor, cancellationToken);
        }
    }
}
