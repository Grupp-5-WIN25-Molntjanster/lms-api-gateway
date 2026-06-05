# LMS API Gateway

The single public entry point for the LMS platform. A **YARP** reverse proxy on
**.NET 10** that fronts all backend microservices (Auth, Verification, Content,
Enrollment, File), validating JWTs once at the edge, forwarding identity to downstream
services, and applying cross-cutting concerns: CORS, rate limiting, correlation IDs, and
structured logging.

Clients only ever talk to the gateway; the gateway talks to the services.

---

## Live deployment

The service is deployed to **API Gateway**:

- Health check → `https://lms-api-gateway-gge8ghc9fgdmdkcp.polandcentral-01.azurewebsites.net/health`

---

## Tech stack

- **.NET 10** / ASP.NET Core (minimal hosting)
- **YARP** (`Yarp.ReverseProxy`) for routing/proxying, configured from `appsettings.json`
- **JWT Bearer** authentication (HMAC-SHA256), shared secret with the Auth service
- **ASP.NET Core Rate Limiting** (fixed-window, partitioned per user/IP)
- **CORS** with an explicit allow-list
- **Serilog** (Console + rolling File sinks)

---

## Architecture

This is a single project (`Lms.ApiGateway`). `Program.cs` wires services through
`Add*` extensions and builds the middleware pipeline; everything else is grouped by
concern.

```
Lms.ApiGateway/
├── Program.cs                 # Bootstrap + middleware pipeline (order matters)
├── appsettings.json           # Serilog, RateLimiting, Jwt, Cors, ReverseProxy (routes/clusters)
├── Authorization/
│   └── Policies.cs            # Policy + role name constants
├── Configuration/             # Strongly-typed options (Jwt, Cors, RateLimit, Gateway)
├── Extensions/                # AddGateway* service registration + pipeline helpers
│   ├── AuthenticationExtensions.cs
│   ├── AuthorizationExtensions.cs
│   ├── CorsExtensions.cs
│   ├── RateLimitingExtensions.cs
│   ├── ReverseProxyExtensions.cs
│   ├── EndpointExtensions.cs       # /health and /error
│   └── LoggingExtensions.cs
└── Middleware/
    ├── CorrelationIdMiddleware.cs  # X-Correlation-Id in/out + log scope
    ├── ClaimsForwardingMiddleware.cs # JWT claims → X-User-* headers
    └── RequestLoggingMiddleware.cs   # method/path/status/duration
```

### Pipeline order (from `Program.cs`)

1. Serilog request logging
2. Correlation ID (`X-Correlation-Id`)
3. Exception handler → `/error`
4. CORS
5. Rate limiter
6. Authentication (validate JWT)
7. Authorization
8. Claims forwarding (`X-User-*` headers)
9. Custom request logging
10. `/health`, `/error`
11. YARP reverse proxy (`MapReverseProxy`)

---

## Routing

YARP routes and clusters are defined in the `ReverseProxy` section of `appsettings.json`.
Each public prefix maps to a cluster whose single destination is the service's base
address (all ending in `/api/`).

| Public prefix | Cluster | Destination |
|---|---|---|
| `/auth/**` | `auth-cluster` | `https://lmsauthapi20260522165735.azurewebsites.net/api/` |
| `/verification/**` | `verification-cluster` | `https://shikoverificationservice-…swedencentral-01.azurewebsites.net/api/` |
| `/content/**` | `content-cluster` | `https://lms-contentservice-api-…germanywestcentral-01.azurewebsites.net/api/` |
| `/enrollments/**` | `enrollment-cluster` | `https://lmsenrollmentserviceapi-…germanywestcentral-01.azurewebsites.net/api/` |
| `/files/**` | `file-cluster` | `http://localhost:5004/api/` |


---

## Security

### Authentication

`AddGatewayAuthentication` configures JWT Bearer validation: issuer, audience, lifetime
(30s clock skew), and HMAC-SHA256 signature, with `NameClaimType = "name"` and
`RoleClaimType = "role"` (matching the tokens issued by the Auth service). Startup throws
if `Jwt:Secret` is empty. Auth success/failure is logged via `JwtBearerEvents`.

### Authorization policies

`Policies.cs` defines `Authenticated`, `InstructorOrAdmin`, and `AdminOnly` (roles:
`Student`, `Instructor`, `Admin`), registered by `AddGatewayAuthorization`. See **Known
issues** — these are defined but not currently attached to any route.

### Claims forwarding

`ClaimsForwardingMiddleware` projects validated JWT claims into request headers for
downstream services: `X-User-Id` (from `sub`/`NameIdentifier`), `X-User-Email`,
`X-User-Roles` (comma-separated), `X-User-Name`. Downstream services should still validate
the JWT for defense in depth. See **Known issues** for the header-spoofing caveat.

### CORS

`AddGatewayCors` allows only the origins in `Cors:AllowedOrigins`, with `AllowCredentials`
(needed for the `Authorization` header) and exposes `X-Correlation-Id`. No wildcard
origins.

### Rate limiting

`AddGatewayRateLimiting` applies a global fixed-window limiter partitioned by the
authenticated user name, falling back to client IP (then `"anonymous"`). On rejection it
returns `429` with a JSON body (`rate_limit_exceeded`, limit, retry-after, timestamp).
Values come from `RateLimiting` in config.

---

## Observability

- **Correlation ID** — reuses an incoming `X-Correlation-Id` or generates one, sets it on
  the request (forwarded downstream) and the response, and pushes it into the Serilog log
  context for distributed tracing.
- **Request logging** — `RequestLoggingMiddleware` logs method, path, status, and duration,
  escalating the log level for 4xx/5xx. Serilog also does its own request logging.
- **Sinks** — Console and a daily rolling file under `logs/` (7-day retention).

---

## Gateway endpoints

The only endpoints the gateway serves directly (everything else is proxied):

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/health` | Anonymous | Status + service metadata (for Azure health probes) |
| `*` | `/error` | Anonymous | Global exception handler target; returns a safe `500` |

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A `Jwt:Secret` matching the Auth service (≥ 32 chars for HS256)

### Run locally

```bash
# from Lms.ApiGateway
dotnet restore
dotnet run
```

The `http` profile listens on `http://localhost:5001` and sets a development
`Jwt__Secret` via `launchSettings.json`. Health check: `http://localhost:5001/health`.

> The `/files` route points at `http://localhost:5004` by default, so a local File
> Service must be running there to exercise it.

---

## Configuration

| Section | Key | Description |
|---|---|---|
| `Jwt` | `Secret` | Shared HS256 signing key. **Required at startup** (set via `Jwt__Secret` env var / Key Vault, never committed) |
| `Jwt` | `Issuer` / `Audience` | Expected token issuer/audience (`lms-auth-service` / `lms-api`) |
| `Cors` | `AllowedOrigins` | Array of allowed frontend origins |
| `RateLimiting` | `PermitLimit` / `WindowInSeconds` / `QueueLimit` | Fixed-window limiter settings |
| `ReverseProxy` | `Routes` / `Clusters` | YARP routing config |
| `Serilog` | … | Console + file sink configuration |

---

## Deployment notes (Grupp 5)

- The gateway is the only service the frontend should call. Add new frontend origins to
  `Cors:AllowedOrigins`.
- `Jwt:Secret` must be identical across the gateway and every downstream service that
  validates tokens; issuer/audience must match the Auth service.
- When adding a new backend service: add a `Route` (public prefix) and a `Cluster`
  (destination ending in `/api/`), and confirm the path math against the **Known issues**
  routing note below.
