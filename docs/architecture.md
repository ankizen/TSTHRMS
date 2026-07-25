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

## Access control (Section 14)

The 4 roles are enforced at the API layer, not just hidden in the UI - `EmployeesController` and
its DTO-shaped children (`export`, `org-chart`, `audit-log`, `documents`) are restricted to
`HRAdmin`/`HRBP`. Manager and Employee never touch that surface at all; they get their own
`/api/my/*` endpoints (`MyProfileController`) that always resolve "who" from the caller's own
`employee_id` JWT claim, never a route parameter - there's no id to spoof.

- **HRBP scope**: `ApplicationUser.AssignedLegalEntityId`/`AssignedProductId` (set at account
  creation, `UsersController`) narrow an HRBP to a single legal entity and/or product; null on
  either means unrestricted on that dimension. `EmployeeService.ApplyHrbpScope` filters every list
  query and `IsHrbpOutOfScope` blocks every single-record read/write (`GetById`, `Create`,
  `Update`, status changes, bank-account reveal) - a scoped HRBP just gets "not found" for a
  record outside their scope, not a distinct "forbidden" response, so scope can't be probed by
  observing the difference. HRAdmin is exempt from every check.
- **Manager**: read-only access to direct reports (`Employee.ReportingManagerId` match) via
  `MyProfileService.GetDirectReportsAsync`, returned as `DirectReportSummaryDto` - a deliberately
  narrow projection (no salary, no bank details, no address/emergency contact) rather than the
  full `EmployeeDto` HR sees.
- **Employee self-service**: `GetOwnProfileAsync` reuses the same masked `EmployeeDto` HR sees for
  the caller's own record (read-only). A small whitelist of contact/emergency fields
  (`EditableEmployeeField`) can be changed only by submitting an `EmployeeEditRequest` through
  `IEmployeeEditRequestService.SubmitAsync`; HR approves or rejects via
  `EmployeeEditRequestsController` (same HRBP-scope rule as everywhere else). Approving applies the
  change through an explicit `switch` over the field enum, not a generic reflection-based setter,
  so a request can never target a field outside the whitelist.
- **User provisioning**: there is no self-registration. `UsersController` (HRAdmin-only) creates a
  login for an *existing* Employee record and assigns its role (and, for HRBP, its scope) -
  `IUserManagementService`/`UserManagementService` mirror the `IAuthService`/`AuthService` split
  (interface in Application, `UserManager<ApplicationUser>`-backed implementation in
  Infrastructure) since only Infrastructure may reference Identity types.

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

Each `AuditLog` row is keyed by the (`EntityName`, `EntityId`) of whatever row actually changed -
a child record like an `EducationRecord` logs under its own id, not the employee's. The Change
History screen (`AuditLogService.GetEmployeeHistoryAsync`) presents one unified per-employee
timeline by first collecting the ids of every child record that belongs to the employee
(education, family, previous employment, identity documents, nominees, standalone documents),
then matching `AuditLog` rows against the employee's own id or any of those child ids. Sensitive
field values come back masked; unmasking a specific entry goes through
`AuditLogService.RevealEntryAsync`, which writes its own `Revealed` audit entry - the same
"mask by default, reveal is an audited action" rule used for the bank account field.

## Bulk import

`EmployeeBulkImportService` reads an uploaded .xlsx workbook (ClosedXML) and both validates and
creates through the same code path: `ValidateAsync` (preview, no writes) and `CommitAsync`
(creates every row that passes) share one parse-and-validate routine, then `CommitAsync` calls
straight into `IEmployeeService.CreateAsync` per valid row rather than duplicating employee
creation logic. Each row runs through the same `EmployeeWriteRequestValidator` used by the
single-employee create endpoint, so business rules (IFSC format, DOB before DOJ, etc.) can't
drift between the two paths. Deliberately covers a practical subset of fields, not the full
`EmployeeWriteRequest` - reporting manager is left out, since resolving one by name would need a
second pass (the manager might be in the same file) and isn't worth the complexity for a first
cut; it can be set afterwards from the edit form.

## Reporting & export

Employee list/export share one filter-building method (`EmployeeService.ApplyFilter`) so the
Excel export can never drift from what the paged list actually shows. Excel generation uses
**ClosedXML** (MIT-licensed) rather than EPPlus, since EPPlus's non-commercial license would be a
legal problem for a product sold to other companies.

## Roadmap

Phase 0 (this foundation - auth, multi-tenancy, app shell) is done. Next is Phase 1, Core HR /
Employee Database, built as 15 reviewable slices per `Core HR_Employee Database Detail.pdf`.
Phases 2-7 (Recruitment, Attendance, Leave, Payroll, Exit, ESS) follow once Core HR is stable,
per `Basic_HR Modules Phase1.pdf`.
