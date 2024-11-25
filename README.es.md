## Componentes y Recursos Utilizados

| Componente                | Descripción                                             | Documentación                                                                 |
|---------------------------|---------------------------------------------------------|-------------------------------------------------------------------------------|
| OpenIDDict                | Framework para control de acceso y autorización en apps | [Documentación OpenIDDict](https://openiddict.com/)                           |
| .NET Core                 | Plataforma de desarrollo para aplicaciones modernas     | [Documentación .NET Core](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview) |
| Postgres                  | Sistema de gestión de bases de datos relacional         | [Documentación Postgres](https://www.postgresql.org/)                         |
| Clean Architecture Template | Plantilla para arquitectura limpia en ASP.NET        | [GitHub - Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture) |


# Servicio de Autorización de TrackHub

El servicio de autorización de TrackHub está construido sobre **OpenIdDict** para el control de acceso a aplicaciones y servicios. A continuación, se describen los métodos de autenticación utilizados y la configuración del sistema.

## Métodos de Autenticación

### Authorization Code Flow PKCE
Se utiliza el flujo de **Authorization Code Flow con PKCE** como método de autenticación para aplicaciones frontend.

### Client Credentials
El flujo de **Client Credentials** se utiliza como método de autenticación para servicios.

## Configuración

La configuración estándar de estos dos métodos se encuentra en las siguientes clases:

- [Program.cs](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/Web/Program.cs)
- [DependencyInjection.cs](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/Web/DependencyInjection.cs)

> **Nota:** Se recomienda siempre el uso de certificados emitidos por una Autoridad de Certificación (CA) en producción. Sin embargo, para efectos de pruebas, se puede generar un certificado local en PowerShell:

```powershell
$cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My"
$password = ConvertTo-SecureString -String "openiddict" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "certificate.pfx" -Password $password
```

## Inicialización de la Base de Datos

La clase [ClientSeeder.cs](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/Web/ClientSeeder.cs) inicializa la base de datos de OpenIdDict con clientes web, móviles y de servicios.

## Interfaz de Usuario

El servicio de autenticación incluye una aplicación ASP.NET que proporciona una pantalla de inicio de sesión. Esta pantalla valida las credenciales a través de la tabla de usuarios en la base de datos y adjunta al token el reclamo de **SID** (Security Identifier Claim), el cual inicia el ciclo de autorización del sistema ([LoginController.cs](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/Web/Controllers/LoginController.cs)).

### Arquitectura del Proyecto

De acuerdo con la estructura del proyecto, la pantalla de inicio de sesión se conecta a la base de datos mediante las capas de **Arquitectura Limpia**, que aprovechan el patrón de **Validación Fluida** para validar y procesar las credenciales. Estas capas están construidas sobre la librería [Common](https://github.com/shernandezp/TrackHubCommon).

## Base de Datos

La tabla de usuarios `users` está alojada en **Postgres** y forma parte del esquema `security`, que es administrado en la sección de [Common](https://github.com/shernandezp/TrackHubSecurity).

## Flujo de la aplicación

1. El usuario ingresa sus credenciales en la pantalla de login de la aplicación web (ASP.NET).
2. La pantalla de login envía las credenciales al **Servicio de Autenticación**.
3. **OpenIdDict** verifica las credenciales contra la **Base de Datos de Usuarios** (Postgres).
4. Si las credenciales son válidas, **OpenIdDict** genera un token que incluye el **SID** (Security Identifier Claim).
5. El token se devuelve a la aplicación web, que permite al usuario acceder a las funcionalidades protegidas de la aplicación.

```plaintext
┌───────────────────────────────────────────────────────────────────┐
│                      Aplicación Web (ASP.NET)                     │
│                      ── Pantalla de Login ──                      │
│                                                                   │
│   ┌──────────────┐                                                │
│   │  Usuario     │                                                │
│   │ Ingresa      │                                                │
│   │ Credenciales │                                                │
│   └──────────────┘                                                │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────┐                                          │
│    │                   │                                          │
│    │ Servicio de       │                                          │
│    │ Autenticación     │                                          │
│    │ ─── OpenIdDict ───│                                          │
│    │                   │                                          │
│    └───────────────────┘                                          │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────────────────────────────────────────────┐  │
│    │                      Base de Datos                        │  │
│    │                        ─── Postgres ───                   │  │
│    │         ┌─────────────────────────────────────┐           │  │
│    │         │                                     │           │  │
│    │         │  Esquema de Seguridad (`security`)  │           │  │
│    │         │ ─── Tabla de Usuarios (`users`) ─── │           │  │
│    │         │                                     │           │  │
│    │         └─────────────────────────────────────┘           │  │
│    └───────────────────────────────────────────────────────────┘  │
│          │                                                        │
│          ▼                                                        │
│   ┌────────────────────────────────────────────────────────────┐  │
│   │    Validación Exitosa:                                     │  │
│   │                                                            │  │
│   │    - Genera Token con SID (Security Identifier Claim)      │  │
│   │    - Devuelve Token a la Aplicación Web                    │  │
│   └────────────────────────────────────────────────────────────┘  │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────────────────────────────────────────────┐  │
│    │  Acceso a Funcionalidades Protegidas de la Aplicación     │  │
│    │       (Si la autenticación fue exitosa)                   │  │
│    └───────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘

───────── Autenticación de Servicios (Client Credentials) ───────────

┌───────────────────────────────────────────────────────────────────┐
│                           Servicio Externo                        │
│                     (Ejemplo: API de Cliente)                     │
│                                                                   │
│   ┌──────────────────────────┐                                    │
│   │  Solicita                │                                    │
│   │  Token                   │                                    │
│   │  (Client Credentials)    │                                    │
│   └──────────────────────────┘                                    │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────┐                                          │
│    │                   │                                          │
│    │ Servicio de       │                                          │
│    │ Autenticación     │                                          │
│    │ ─── OpenIdDict ───│                                          │
│    │                   │                                          │
│    └───────────────────┘                                          │
│          │                                                        │
│          ▼                                                        │
│    ┌───────────────────────────────────────────────────────────┐  │
│    │   Validación de Credenciales del Servicio Externo         │  │
│    │                                                           │  │
│    │   - Genera Token de Servicio (Client Credentials Flow)    │  │
│    │   - No incluye SID, pero contiene permisos de acceso      │  │
│    │                                                           │  │
│    └───────────────────────────────────────────────────────────┘  │
│          │                                                        │
│          ▼                                                        │
│   ┌────────────────────────────────────────────────────────────┐  │
│   │   Token devuelto al Servicio Externo para autenticación    │  │
│   │   y acceso a recursos protegidos                           │  │
│   └────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

Notas:
- El **Método Client Credentials** permite a servicios externos autenticarse y obtener un token de acceso sin necesidad de credenciales de usuario.
- El **Servicio de Autenticación (OpenIdDict)** gestiona tanto el flujo de autorización para usuarios como el flujo de client credentials para servicios.
- La **Base de Datos Postgres** almacena la información de seguridad para usuarios en el esquema `security` y la tabla `users`.

## Licencia

Este proyecto está bajo la Licencia Apache 2.0. Consulta el archivo [LICENSE](https://www.apache.org/licenses/LICENSE-2.0) para más información.
