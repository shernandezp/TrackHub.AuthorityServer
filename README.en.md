﻿## Components and Resources

| Component                 | Description                                             | Documentation                                                                 |
|---------------------------|---------------------------------------------------------|-------------------------------------------------------------------------------|
| OpenIDDict                | Framework for access control and authorization | [Documentación OpenIDDict](https://openiddict.com/)                           |
| .NET Core 8               | Development platform for modern applications   | [Documentación .NET Core 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/overview) |
| Postgres                  | Relational database management system          | [Documentación Postgres](https://www.postgresql.org/)                         |
| Clean Architecture Template | Template for ASP.NET clean architecture      | [GitHub - Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture) |


# TrackHub Authorization Service

The TrackHub Authorization Service leverages OpenIdDict for access control across applications and services. The following sections describe the authentication methods and system configuration.

## Authentication Methods

### Authorization Code Flow with PKCE
This **Authorization Code Flow con PKCE** is used for authenticating frontend applications.

### Client Credentials Flow
The **Client Credentials Flow** provides authentication for backend services.

## Configuration

Standard configurations for these flows are set in the following classes:

- [Program.cs](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/Web/Program.cs)
- [DependencyInjection.cs](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/Web/DependencyInjection.cs)

> **Note:** For production, it is recommended to use a certificate issued by a Certificate Authority (CA). For testing, a self-signed certificate can be generated using PowerShell:

```powershell
$cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My"
$password = ConvertTo-SecureString -String "openiddict" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "certificate.pfx" -Password $password
```

## Database Initialization

The [ClientSeeder.cs](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/Web/ClientSeeder.cs) class initializes the OpenIdDict database with clients for web, mobile, and backend services.

## User Interface

The authorization service includes an ASP.NET application with a login screen. This screen validates credentials from the user table in the database and attaches the SID (Security Identifier Claim) to the token, which initiates the authorization cycle ([LoginController.cs](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/Web/Controllers/LoginController.cs)).

### Project Architecture

The login screen interfaces with the database using Clean Architecture layers that employ Fluent Validation for credential validation and processing. These layers are built on the [Common](https://github.com/shernandezp/TrackHubCommon).

## Database

The `users` table resides in Postgres within the `security` schema, managed in the [Common](https://github.com/shernandezp/TrackHubSecurity).

## Application Flow

1. The user enters their credentials on the login screen of the web application (ASP.NET).
2. The login screen submits the credentials to the **Authorization Service**.
3. **OpenIdDict** verifies the credentials against the **Security Database** (Postgres).
4. If valid, **OpenIdDict** generates a token that includes the SID (Security Identifier Claim).
5. The token is returned to the web application, granting the user access to protected features.

```plaintext
┌───────────────────────────────────────────────────────────────────┐
│                      Web Application (ASP.NET)                    │
│                      ── Login Screen ──                           │
│                                                                   │
│   ┌──────────────┐                                                │
│   │  User        │                                                │
│   │ Submits      │                                                │
│   │ Credentials  │                                                │
│   └──────────────┘                                                │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────┐                                          │
│    │                   │                                          │
│    │ Authorization     │                                          │
│    │ Service           │                                          │
│    │ ─── OpenIdDict ───│                                          │
│    │                   │                                          │
│    └───────────────────┘                                          │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────────────────────────────────────────────┐  │
│    │                      Database                             │  │
│    │                        ─── Postgres ───                   │  │
│    │         ┌─────────────────────────────────────┐           │  │
│    │         │                                     │           │  │
│    │         │  Security Schema (`security`)       │           │  │
│    │         │ ─── Users Table (`users`) ───       │           │  │
│    │         │                                     │           │  │
│    │         └─────────────────────────────────────┘           │  │
│    └───────────────────────────────────────────────────────────┘  │
│          │                                                        │
│          ▼                                                        │
│   ┌────────────────────────────────────────────────────────────┐  │
│   │    Successful Validation:                                  │  │
│   │                                                            │  │
│   │    - Token Generated with SID (Security Identifier Claim)  │  │
│   │    - Token Returned to Web Application                     │  │
│   └────────────────────────────────────────────────────────────┘  │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────────────────────────────────────────────┐  │
│    │  Access to Protected Application Features                 │  │
│    │       (Upon Successful Authentication)                    │  │
│    └───────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘

───────── Service Authentication (Client Credentials Flow) ─────────

┌───────────────────────────────────────────────────────────────────┐
│                           External Service                        │
│                     (Example: Client API)                         │
│                                                                   │
│   ┌──────────────────────────┐                                    │
│   │  Requests                │                                    │
│   │  Token                   │                                    │
│   │  (Client Credentials)    │                                    │
│   └──────────────────────────┘                                    │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────┐                                          │
│    │                   │                                          │
│    │ Authorization     │                                          │
│    │ Service           │                                          │
│    │ ─── OpenIdDict ───│                                          │
│    │                   │                                          │
│    └───────────────────┘                                          │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────────────────────────────────────────────┐  │
│    │   External Service Credential Validation                  │  │
│    │                                                           │  │
│    │   - Service Token Generated (Client Credentials Flow)     │  │
│    │   - Contains Access Permissions                           │  │
│    │                                                           │  │
│    └───────────────────────────────────────────────────────────┘  │
│          │                                                        │
│          ▼                                                        │
│   ┌────────────────────────────────────────────────────────────┐  │
│   │   Token returned to External Service for authentication    │  │
│   │   and access to protected resources                        │  │
│   └────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

Key Points:
- The **Client Credentials Flow** enables external services to authenticate without user credentials.
- The **Authorization Service (OpenIdDict)** handles both user authorization and service authentication flows.
- **Postgres Database** stores security information for users within the `security` schema and `users` table.

## License

This project is licensed under the Apache 2.0 License. See the [LICENSE file](https://www.apache.org/licenses/LICENSE-2.0) for more information.
