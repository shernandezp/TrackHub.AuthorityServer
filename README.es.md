# Servicio de Autorización de TrackHub

## Características Principales

- **OAuth 2.0 y OpenID Connect**: Cumplimiento total con protocolos de autenticación estándar de la industria usando OpenIdDict
- **Flujo de Código de Autorización con PKCE**: Autenticación segura para aplicaciones SPA y móviles
- **Flujo de Credenciales de Cliente**: Autenticación máquina a máquina para microservicios backend
- **Gestión de Tokens**: Capacidades de generación, validación y revocación de tokens JWT con delimitación por audiencia
- **Soporte Multi-Cliente**: Clientes OAuth configurables para aplicaciones web, móviles y de servicios con asignación de audiencia basada en recursos
- **Arquitectura Limpia**: Arquitectura en capas que promueve la mantenibilidad y la capacidad de prueba
- **Integración PostgreSQL**: Almacenamiento seguro de credenciales de usuario con hash de contraseñas BCrypt
- **UI de Login Personalizable**: Interfaz de inicio de sesión basada en ASP.NET con soporte para personalización de marca

---

## Inicio Rápido

### Requisitos Previos

- .NET 10.0 SDK
- PostgreSQL 14+
- Certificado SSL (autofirmado para desarrollo, emitido por CA para producción)

### Instalación

1. **Clonar el repositorio**:
   ```bash
   git clone https://github.com/shernandezp/TrackHub.AuthorityServer.git
   cd TrackHub.AuthorityServer
   ```

2. **Configurar la conexión a la base de datos** en `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "SecurityConnection": "Host=localhost;Database=trackhub_security;Username=postgres;Password=yourpassword"
     }
   }
   ```

3. **Generar un certificado autofirmado** (solo desarrollo):
   ```powershell
   $cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My"
   $password = ConvertTo-SecureString -String "openiddict" -Force -AsPlainText
   Export-PfxCertificate -Cert $cert -FilePath "certificate.pfx" -Password $password
   ```

4. **Ejecutar las migraciones de la base de datos**:
   ```bash
   dotnet ef database update
   ```

5. **Iniciar la aplicación**:
   ```bash
   dotnet run --project src/Web
   ```

6. **Acceder a la página de login** en `https://localhost:5001`

---

## Componentes y Recursos Utilizados

| Componente                | Descripción                                             | Documentación                                                                 |
|---------------------------|---------------------------------------------------------|-------------------------------------------------------------------------------|
| OpenIDDict                | Framework para control de acceso y autorización en apps | [Documentación OpenIDDict](https://openiddict.com/)                           |
| .NET Core                 | Plataforma de desarrollo para aplicaciones modernas     | [Documentación .NET Core](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview) |
| Postgres                  | Sistema de gestión de bases de datos relacional         | [Documentación Postgres](https://www.postgresql.org/)                         |
| Clean Architecture Template | Plantilla para arquitectura limpia en ASP.NET        | [GitHub - Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture) |

---

## Descripción General

El servicio de autorización de TrackHub está construido sobre **OpenIdDict** para el control de acceso a aplicaciones y servicios. A continuación, se describen los métodos de autenticación utilizados y la configuración del sistema.

## Métodos de Autenticación

### Authorization Code Flow PKCE
Se utiliza el flujo de **Authorization Code Flow con PKCE** como método de autenticación para aplicaciones frontend.

### Client Credentials
El flujo de **Client Credentials** se utiliza como método de autenticación para servicios.

Los tokens emitidos por este flujo incluyen `sub`, `role=service`, `client_id`,
`principal_type=ServiceClient` y —cuando el cliente está asociado a un tenant— el claim
`account_id`. La cuenta **no** la envía el llamante: se deriva de las filas activas y vigentes del
cliente en `security.service_client_permissions`, de modo que una credencial de socio no puede
ampliar su propio alcance.

| Permisos vigentes del cliente | Claim `account_id` |
|---|---|
| Al menos un permiso con `allowcrossaccount` (identidades internas de la plataforma: `router_client`, `syncworker_client`, `security_client`, `geofence_client`, `trip_client`) | **No se emite** — el token queda sin cuenta, que es contra lo que coincide un permiso global. |
| Permisos que nombran exactamente una cuenta (caso socio/TMS) | Se emite automáticamente para esa cuenta. |
| Permisos que nombran varias cuentas | **Requiere el parámetro `account_id`** (ver abajo). |
| Ningún permiso asociado a una cuenta | No se emite. |

#### El parámetro opcional `account_id`

Un cliente de servicio cuyos permisos abarcan **más de una cuenta** es ambiguo: el endpoint de token
no adivina el tenant, porque emitir uno arbitrario apuntaría en silencio al cliente equivocado. Ese
tipo de cliente debe indicar el tenant en cada solicitud de token:

```bash
curl -X POST https://<authority>/connect/token \
  -d grant_type=client_credentials \
  -d client_id=<partner_client> \
  -d client_secret=<secret> \
  -d scope=service_scope \
  -d account_id=<guid-de-cuenta>
```

El parámetro es **opcional e innecesario para clientes de una sola cuenta y para los internos de la
plataforma**, y sólo puede restringir, nunca ampliar: la cuenta solicitada debe corresponder a un
permiso activo del propio cliente. El endpoint responde `invalid_request` cuando el valor no es un
identificador válido, cuando el cliente no tiene un permiso activo para la cuenta solicitada, o
cuando un cliente multicuenta lo omite. Este último caso es un rechazo deliberado, no una falla:
agregue el parámetro o asigne al cliente permisos de una sola cuenta.

> **Aprovisionamiento de un cliente socio:** siembre sus filas en
> `security.service_client_permissions` **con** `accountid` y **sin** `allowcrossaccount`, y vuelva
> a ejecutar `db-init`. Un cliente sembrado como interno (`allowcrossaccount = true`) no recibe
> claim de cuenta y se trata como identidad de plataforma.

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

El proyecto [ClientSeeder](https://github.com/shernandezp/TrackHub.AuthorityServer/blob/master/src/ClientSeeder/Seeder.cs) inicializa la base de datos de OpenIdDict con clientes web, móviles y de servicios.

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


