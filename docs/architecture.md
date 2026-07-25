# Architecture

TSTHRMS is a multi-tenant HR Management System. It runs The Thiinker / ThinkerSteps' own HR
today and is architected to be sold as a SaaS product to other companies without a rewrite.

## Stack

| Layer | Choice |
|---|---|
| Backend | ASP.NET Core 10 (C# 14), layered: Domain / Application / Infrastructure / Api |
| Database | MySQL 8.4, via EF Core 9.0.x + Pomelo.EntityFrameworkCore.MySql |
| Auth | ASP.NET Core Identity + JWT access token + rotating refresh token (HttpOnly cookie) |
| Frontend | React 19 + TypeScript + Vite, Tailwind CSS + shadcn/ui, Lucide icons |
| Data/forms | TanStack Query + TanStack Table, React Hook Form + Zod |
| Hosting | Windows Server, IIS (ANCMv2 -> Kestrel), single origin (SPA + API on one site) |

**EF Core / Pomelo version note**: Pomelo's MySQL provider has not shipped EF Core 10 support
yet, so the data-access layer is deliberately pinned to EF Core 9.0.x while the app itself runs
on the .NET 10 / ASP.NET Core 10 runtime. This is a supported, low-risk combination. Revisit
the pin once Pomelo ships EF Core 10 support.

## Layering

```
TSTHRMS.Domain          - entities, enums, no framework dependencies
TSTHRMS.Application     - use-cases, DTOs, interfaces (IApplicationDbContext, ITenantContext,
                          ICurrentUserService, IAuthService), FluentValidation validators
TSTHRMS.Infrastructure  - EF Core DbContext, migrations, Identity stores, JWT/auth services,
                          the audit-log SaveChanges interceptor
TSTHRMS.Api             - composition root, controllers, JWT bearer wiring, middleware
```

Dependencies point inward (Api -> Infrastructure -> Application -> Domain). Identity's
`ApplicationUser`/`ApplicationRole` live in Infrastructure, not Domain, so Domain stays free
of any ASP.NET Core Identity package reference.

## Multi-tenancy

- **Tenant** = a subscribing customer/company. Your own company is tenant #1; a future
  customer who buys the product becomes a new, fully isolated tenant.
- **LegalEntity** = a legal entity *within* a tenant (The Thiinker, ThinkerSteps) - this is
  the Core HR spec's "Entity" field, unchanged, just nested one level under Tenant.
- **Product** = the existing cost-tag (SwarnApp/JewelSteps/Miniz), independent of LegalEntity.

Implementation (`TSTHRMS.Infrastructure/Persistence/ApplicationDbContext.cs`):
- Every tenant-scoped table implements `ITenantScoped` (a `TenantId` property) by inheriting
  `TenantScopedEntity`.
- `OnModelCreating` walks every entity type in the model and applies a global EF Core query
  filter (`HasQueryFilter`) to any `ITenantScoped` entity automatically, via reflection - a new
  table can't accidentally skip tenant filtering just because a developer forgot to opt in.
- `SaveChangesAsync` auto-stamps `TenantId` (and `CreatedAt`/`CreatedBy`/`ModifiedAt`/`ModifiedBy`)
  on insert/update, so application code never sets `TenantId` by hand.
- `ITenantContext` resolves the current tenant from the authenticated JWT's `tenant_id` claim
  (implemented in `TSTHRMS.Api/Services/TenantContext.cs`, since it needs `IHttpContextAccessor`).
- The test that must never go red: `TSTHRMS.IntegrationTests/Persistence/TenantIsolationTests.cs`
  - asserts a query scoped to one tenant never returns another tenant's rows.

## Auth

- Login returns a short-lived JWT access token (15 min) in the response body and sets the
  refresh token as an **HttpOnly, SameSite=Strict** cookie (`tsthrms_refresh`, scoped to
  `/api/auth`) - never exposed to JS, so an XSS payload can't exfiltrate a long-lived credential.
- Refresh tokens are stored server-side only as a SHA-256 hash (`ApplicationUser.RefreshTokenHash`)
  and rotate on every use (`AuthService.IssueTokensAsync` overwrites the hash each time).
- The frontend keeps the access token in memory only (Zustand store, no localStorage) and
  silently calls `/api/auth/refresh` once on app boot to restore a session from the cookie.
- Roles are fixed to the Core HR spec's 4 access levels: `HRAdmin`, `HRBP`, `Manager`, `Employee`
  (`TSTHRMS.Application/Common/RoleNames.cs`).

## File storage

`IFileStorageService` stores bytes behind an opaque, always-server-generated key (never a
user-supplied path) so callers can't path-traverse and the implementation can swap from local
disk to blob storage later without touching anything above the interface. `Document` (metadata:
filename, content type, size, uploaded by/at) is deliberately generic - it's the seed of the
full Document Repository (Core HR Section 10, a later slice); Education certificates are its
first consumer. Production must point `FileStorage:RootPath` outside the deployed site folder
(see the deployment runbook) so redeploys never delete uploaded files.

## Audit logging

`AuditSaveChangesInterceptor` captures a field-level change record for every create/update/delete
of an `AuditableEntity`. Values are stored **unmasked** (compliance needs the true history);
masking sensitive fields (bank details, PAN, Aadhaar - once Core HR adds them) is a read-time
concern applied when the audit log is displayed, driven by the `[Sensitive]` attribute
(`TSTHRMS.Domain/Common/SensitiveAttribute.cs`) rather than baked into storage.

## Roadmap

Phase 0 (this foundation - auth, multi-tenancy, app shell) is done. Next is Phase 1, Core HR /
Employee Database, built as 15 reviewable slices per `Core HR_Employee Database Detail.pdf`.
Phases 2-7 (Recruitment, Attendance, Leave, Payroll, Exit, ESS) follow once Core HR is stable,
per `Basic_HR Modules Phase1.pdf`.
