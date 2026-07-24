# TrackHub Authorization Service

## Key Features

- **OAuth 2.0 & OpenID Connect**: Full compliance with industry-standard authentication protocols using OpenIdDict
- **Authorization Code Flow with PKCE**: Secure authentication for SPA and mobile applications
- **Client Credentials Flow**: Machine-to-machine authentication for backend microservices
- **Token Management**: JWT token generation, validation, and revocation capabilities with audience-based scoping
- **Multi-Client Support**: Configurable OAuth clients for web, mobile, and service applications with resource-based audience assignment
- **Clean Architecture**: Layered architecture promoting maintainability and testability
- **PostgreSQL Integration**: Secure user credential storage with BCrypt password hashing
- **Customizable Login UI**: ASP.NET-based login interface with branding support

---

## Quick Start

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL 14+
- SSL Certificate (self-signed for development, CA-issued for production)

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/shernandezp/TrackHub.AuthorityServer.git
   cd TrackHub.AuthorityServer
   ```

2. **Configure the database connection** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "SecurityConnection": "Host=localhost;Database=trackhub_security;Username=postgres;Password=yourpassword"
     }
   }
   ```

3. **Generate a self-signed certificate** (development only):
   ```powershell
   $cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My"
   $password = ConvertTo-SecureString -String "openiddict" -Force -AsPlainText
   Export-PfxCertificate -Cert $cert -FilePath "certificate.pfx" -Password $password
   ```

4. **Run database migrations**:
   ```bash
   dotnet ef database update
   ```

5. **Start the application**:
   ```bash
   dotnet run --project src/Web
   ```

6. **Access the login page** at `https://localhost:5001`

---

## Components and Resources

| Component                 | Description                                             | Documentation                                                                 |
|---------------------------|---------------------------------------------------------|-------------------------------------------------------------------------------|
| OpenIDDict                | Framework for access control and authorization | [OpenIDDict Documentation](https://openiddict.com/)                           |
| .NET Core                 | Development platform for modern applications   | [.NET Core Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview) |
| Postgres                  | Relational database management system          | [Postgres Documentation](https://www.postgresql.org/)                         |
| Clean Architecture Template | Template for ASP.NET clean architecture      | [GitHub - Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture) |

---

## Overview

The TrackHub Authorization Service leverages OpenIdDict for access control across applications and services. The following sections describe the authentication methods and system configuration.

## Authentication Methods

### Authorization Code Flow with PKCE
This **Authorization Code Flow con PKCE** is used for authenticating frontend applications.

### Client Credentials Flow
The **Client Credentials Flow** provides authentication for backend services.

Tokens issued by this flow carry `sub`, `role=service`, `client_id`, `principal_type=ServiceClient`
and — when the client is tenant-bound — an `account_id` claim. The account is **not** supplied by
the caller: it is derived from the client's active, in-effect rows in
`security.service_client_permissions`, so a partner credential cannot widen its own reach.

| Client's effective grants | `account_id` claim |
|---|---|
| At least one `allowcrossaccount` grant (the platform-internal identities: `router_client`, `syncworker_client`, `security_client`, `geofence_client`, `trip_client`) | **Not issued** — the token stays unscoped, which is what a platform-wide grant matches against. |
| Grants naming exactly one account (the partner/TMS case) | Issued automatically for that account. |
| Grants naming several accounts | **Requires the `account_id` request parameter** (see below). |
| No account-bound grant | Not issued. |

#### The optional `account_id` request parameter

A service client whose grants span **more than one account** is ambiguous: the token endpoint will
not guess a tenant, because issuing an arbitrary one would silently point a partner at the wrong
customer. Such a client must name the tenant it wants on each token request:

```bash
curl -X POST https://<authority>/connect/token \
  -d grant_type=client_credentials \
  -d client_id=<partner_client> \
  -d client_secret=<secret> \
  -d scope=service_scope \
  -d account_id=<account-guid>
```

The parameter is **optional and unnecessary for single-account and platform-internal clients**, and
it can only narrow, never widen: the requested account must match one of the client's own active
grants. The endpoint answers `invalid_request` when the value is not a valid identifier, when the
client holds no active grant for the requested account, or when a multi-account client omits it
entirely. That last case is a deliberate rejection rather than a fault — add the parameter, or seed
the client with a single account grant.

> **Provisioning a partner client:** seed its rows in `security.service_client_permissions` **with**
> an `accountid` and **without** `allowcrossaccount`, then re-run `db-init`. A client seeded the
> platform way (`allowcrossaccount = true`) receives no account claim and is treated as a
> platform-internal identity.

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

The [ClientSeeder](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/ClientSeeder/Seeder.cs) project initializes the OpenIdDict database with clients for web, mobile, and backend services.

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

## Docker Deployment

When deployed behind a reverse proxy (e.g., nginx with SSL termination), the Authority Server uses `ForwardedHeaders` middleware to trust `X-Forwarded-Proto` and `X-Forwarded-For` headers. This ensures OpenIddict correctly identifies HTTPS requests even though internal container traffic uses HTTP.

See [TrackHub.Deployment](https://github.com/shernandezp/TrackHub.Deployment) for full deployment instructions.

## License

This project is licensed under the Apache 2.0 License. See the [LICENSE file](https://www.apache.org/licenses/LICENSE-2.0) for more information.



