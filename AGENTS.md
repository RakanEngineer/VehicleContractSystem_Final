# Vehicle Contract Management System

ASP.NET Core MVC + Entity Framework Core + SQL Server

These project-specific instructions complement the shared [Engineering Standards v1.0](https://github.com/RakanEngineer/EngineeringStandards/blob/main/README.md). Apply the shared baseline proportionately to this system's legal, financial, privacy, operational, and lifecycle risks. Project-specific rules add detail; they do not authorize new product scope or planned modules.

---

# PROJECT ARCHITECTURE

## Architecture Pattern

- ASP.NET Core MVC
- Entity Framework Core
- Identity Authentication
- Repository pattern is NOT used currently
- DbContext is used directly in controllers/services

Preserve the current architecture unless a concrete requirement justifies a gradual, reviewed change. Do not introduce Repository Pattern, CQRS, MediatR, microservices, or other abstractions merely for consistency with another project.

---

# MAIN LAYERS

## Controllers

Handle:

- HTTP requests
- Validation
- ViewModels
- Authorization
- Service orchestration

Controllers MUST NOT:

- contain complex business logic
- contain duplicated logic
- access database repeatedly unnecessarily

## Models

Contain:

- entities
- relationships
- validation attributes

## ViewModels

Used ONLY for:

- UI forms
- dropdown lists
- combined page data

Never expose database entities directly to complex forms.

## Services

Contain:

- payment logic
- business rules
- calculations
- reusable workflows

## Data

ApplicationDbContext contains:

- relationships
- constraints
- seed data
- indexes

---

# DATABASE RULES

## Foreign Keys

Always use:

```csharp
.OnDelete(DeleteBehavior.Restrict)
```

unless cascade delete is absolutely required.

Never create multiple cascade paths.

## Decimal Fields

Always use:

```csharp
[Column(TypeName = "decimal(18,2)")]
```

for money values.

Use decimal arithmetic for monetary values. Avoid floating-point types for amounts. Review rounding, precision, currency assumptions, and calculations at business boundaries.

## Computed Properties

Use read-only expressions:

```csharp
public decimal RemainingAmount => VehiclePrice - PaidAmount;
```

Never assign values manually to computed properties.

## Data integrity and query performance

- Preserve referential integrity with explicit relationships, constraints, and appropriate transactions.
- Use concurrency control where simultaneous contract or payment updates could conflict.
- Prevent N+1 query patterns and project only the fields needed.
- Use `AsNoTracking()` for read-only EF Core queries when tracking is unnecessary.
- Use stable ordering and pagination for potentially large contract, vehicle, payment, and administrative lists.
- Add indexes based on actual lookup, filtering, sorting, and relationship patterns; inspect generated SQL or query plans when performance requires it.
- Avoid loading full entity graphs when a ViewModel projection is sufficient.
- Measure before introducing caching or additional infrastructure. If caching is justified, define invalidation and expiration behavior.
- Keep connection strings and credentials out of source code and version control.

---

# LOCALIZATION RULES

Current project language:

- Arabic only

DO NOT:

- use CultureInfo logic
- use dynamic language switching
- use localization middleware unless required

Always use:

- NameAr
- Arabic labels
- Arabic validation messages

Do not use:

- Name property with culture switching

Example:

```csharp
@province.NameAr
```

NOT:

```csharp
@province.Name
```

---

# VIEW RULES

## Razor Views

- Use Bootstrap 5
- RTL layout only
- Arabic UI only

## asp-for

Always bind to actual scalar properties.

Correct:

```html
<select asp-for="Contract.Vehicle.VehicleTypeId">
```

Wrong:

```html
<select asp-for="Contract.Vehicle.VehicleType">
```

## Validation

Always include the appropriate validation message element for validated fields, for example:

```html
<span asp-validation-for="PropertyName"></span>
```

Use validation summaries where appropriate. Do not leave an empty `asp-validation-for` expression.

---

# CODING STANDARDS

## Naming

### Classes

PascalCase

Examples:

- ContractController
- VehiclePaymentService

### Methods

PascalCase

### Private Fields

_prefixCamelCase

Example:

```csharp
private readonly ApplicationDbContext _context;
```

### Variables

camelCase

---

# NULLABILITY RULES

Never suppress warnings blindly.

Always:

- initialize collections
- initialize required navigation properties
- use nullable types correctly

Example:

```csharp
public Vehicle Vehicle { get; set; } = null!;
```

---

# EF CORE RULES

Always configure relationships explicitly.

Example:

```csharp
builder.Entity<VehicleModel>()
    .HasOne(vm => vm.VehicleType)
    .WithMany(vt => vt.VehicleModels)
    .HasForeignKey(vm => vm.VehicleTypeId)
    .OnDelete(DeleteBehavior.Restrict);
```

Review query shape, generated SQL, indexes, pagination, and tracking behavior for important data access paths. Use transactions for operations that must succeed or fail atomically.

---

# UI RULES

- Arabic text only
- RTL only
- Consistent Bootstrap styling
- Use cards and responsive layout
- Use validation summaries

---

# SECURITY AND PRIVACY RULES

The system handles identity information, contact details, vehicle identifiers, contracts, payment records, and uploaded documents. Treat these as sensitive business and personal data.

## Authentication and authorization

- Enforce authentication and server-side authorization on every protected action.
- Apply least privilege. Verify that the authenticated user is permitted to access the specific contract, vehicle, payment, or document, not merely that the user is signed in.
- Preserve and consistently apply the existing contract access checks (including `CanAccessContract` where applicable) to view, edit, export, download, and other resource operations.
- Verify office, user, and resource ownership/assignment on the server. Never trust client-supplied IDs or ownership claims as proof of access.
- Protect administrative operations with explicit role/permission checks.
- Include negative tests for cross-user and cross-office access.
- Review Identity password policy, lockout, account verification, password reset, cookie/session security, and revocation behavior when authentication code changes.
- Use HTTPS and secure cookie settings appropriate to each environment. Do not expose credentials or session identifiers in URLs or logs.

## Sensitive data

- Collect and display only the personal and business information required for the workflow.
- Protect identity numbers, phone numbers, addresses, contract data, and payment details in transit, at rest, logs, exports, and backups as appropriate.
- Do not log passwords, tokens, connection strings, API keys, full identity numbers, or unnecessary personal data.
- Define appropriate access, retention, and deletion practices for sensitive records, subject to legal and business requirements.
- Never use real personal or financial data in test fixtures or examples unless it is explicitly approved and appropriately protected.

## Document upload, storage, and download

- Validate uploaded file size, allowed extensions, actual file content/signature, and generated storage names on the server. Do not trust the supplied filename or MIME type.
- Store uploads outside publicly executable web roots and prevent path traversal and direct unauthenticated access.
- Apply malware scanning where configured. A document with a pending, failed, or otherwise unapproved scan status must not be made available for download or processing until the required scan has passed.
- Verify the caller's authorization for the parent contract/document on every download, preview, replacement, and deletion request.
- Use safe content-disposition and content-type handling; avoid rendering untrusted active content inline.
- Protect stored files with access controls and backups appropriate to their sensitivity.
- Do not silently bypass scanning or validation when a scanning service is unavailable; fail safely and record an operationally useful status.

## Web security

- Validate and normalize untrusted input on the server; client-side validation is for usability only.
- Preserve anti-forgery protection for state-changing MVC requests.
- Use explicit, environment-specific CORS rules if cross-origin access is introduced.
- Apply rate limiting to login, recovery, upload, and other abuse-prone endpoints where appropriate.
- Avoid returning internal exception messages, stack traces, SQL details, filesystem paths, or secrets to users.
- Keep security headers and Content Security Policy deliberate; do not weaken them to work around UI issues without review.
- Review dependency vulnerability alerts and update affected packages through a focused, tested change.

---

# CONTRACT AND FINANCIAL INTEGRITY

Contracts are legally significant records.

- Preserve immutable contract snapshot values for historical display, printing, and PDF export. Do not silently replace historical snapshot values with current Vehicle data.
- Ensure snapshot values are captured consistently at the contract creation boundary.
- Treat contract status changes, payment recording, and financial calculations as business operations with explicit validation and authorization.
- Prevent duplicate payment processing and inconsistent balance updates. Use transactions, concurrency handling, and idempotency where appropriate.
- Keep an auditable record of important contract and payment changes, including actor and timestamp where the existing domain supports it.
- Do not change historical financial or contract data through broad updates or destructive migrations without an approved, verified plan.
- Test rounding, partial payments, overpayments, cancellation, and concurrent updates where those workflows exist.

---

# TESTING AND VERIFICATION

Use the test structure already present in the repository; do not assume or introduce a test framework without inspecting the solution first.

For changes, select appropriate checks:

- Unit tests for calculations, validation, contract rules, and payment workflows.
- Integration tests for EF Core persistence, transactions, Identity, and file-storage boundaries where practical.
- MVC/API tests for validation, authentication, authorization, and response behavior where applicable.
- Security tests for unauthorized contract access, cross-office access, document access, upload validation, and scan-status enforcement.
- Regression tests for contract snapshots, PDF/export output, payment balances, and critical workflows.
- Test failure paths, boundary values, duplicate requests, and concurrency cases.

Run the relevant restore, build, and tests after changes. Report the exact commands and results. A successful build alone is not proof that behavior or authorization is correct.

---

# LOGGING, MONITORING, AND RELIABILITY

Monitoring is continuous, not a one-time setup.

- Use structured logs with appropriate severity and correlation/trace identifiers.
- Monitor application errors, request latency, database performance, failed authentication attempts, document scanning failures, and critical contract/payment workflow failures.
- Use health checks for critical dependencies where appropriate.
- Do not log credentials, tokens, sensitive document contents, or unnecessary personal/financial data.
- Handle external service timeouts and failures deliberately; do not report a document as scanned or a payment as completed when confirmation is missing.
- Review operational signals after deployments and incidents.
- Maintain appropriate backup and restore procedures for the database and document storage, and verify recovery periodically.

---

# CI/CD AND DEPLOYMENT

- Keep development, test, staging, and production configuration separated.
- Store environment-specific secrets outside source control.
- Run automated restore/build and relevant test checks in CI.
- Review deployment impact and database migration SQL before release.
- Test migrations and recovery procedures outside production first.
- Do not run production database migrations, destructive operations, or releases without explicit approval for the specific action.
- Use health checks and verify critical application behavior after deployment.
- Maintain practical rollback and recovery instructions.
- Do not deploy when required checks fail or the release risk is not understood.

---

# IMPORTANT PROJECT RULES

Before modifying code:

1. Understand dependencies.
2. Search related models/controllers/views/services and tests.
3. Inspect the current branch, working tree, and existing local changes.
4. Preserve existing functionality.
5. Avoid breaking migrations.
6. Avoid duplicate code.
7. Define a narrow change scope and explain broad or architectural impact before implementation.

Never:

- create hacky fixes
- duplicate logic
- ignore nullable warnings
- remove relationships blindly
- introduce architecture patterns without a concrete need
- weaken authorization or validation to make a workflow pass
- expose secrets or sensitive user data

---

# MIGRATION RULES

Before creating migrations:

- verify relationships
- verify FK delete behaviors
- verify seed data IDs
- review generated migration code and SQL
- identify data-loss, locking, and compatibility risks
- confirm the target environment and backup/recovery plan

For potentially destructive or breaking schema changes:

1. Add nullable columns or compatible structures first where possible.
2. Backfill data with a controlled, repeatable process.
3. Verify data integrity and application compatibility.
4. Enforce constraints only after verification.
5. Remove obsolete structures only in a separately reviewed step after confirming they are no longer needed.

Never drop old columns before migration verification.

After a local or non-production migration, run the applicable build and migration checks and verify the resulting schema/data. `Update-Database` is not an instruction to update production automatically. Production migrations require separate explicit approval.

---

# GOAL

Maintain:

- clean and understandable architecture
- stable and consistent database
- secure contract and document access
- accurate financial records
- immutable historical contract snapshots
- production-ready code
- Arabic-first vehicle contract management system

---

# FUTURE ARCHITECTURE ROADMAP

## Long-Term System Direction

The system is evolving toward:

```text
Vehicle-centric architecture
with immutable contract snapshots
```

Similar to professional vehicle marketplace systems.

This roadmap describes direction only. It does not authorize implementing new modules or changing current scope without an explicit request.

## Vehicle Entity Rules

Vehicle is a real persistent entity.

Vehicle must contain:

- VehicleTypeId
- VehicleModelId
- ColorId
- LicenseNumber
- ChassisNumber
- ManufactureYear
- OwnerId
- Status (future-ready)

Vehicle data must be reusable across:

- contracts
- listings
- ownership transfer
- marketplace modules

## Contract Snapshot Rules

Contracts are legal immutable documents.

Contract MUST store snapshot fields copied from Vehicle at creation time.

Examples:

- VehicleTypeNameSnapshot
- VehicleModelNameSnapshot
- VehicleColorNameSnapshot
- LicenseNumberSnapshot
- ChassisNumberSnapshot
- ManufactureYearSnapshot

Never rely on live Vehicle data when:

- printing contracts
- exporting PDFs
- viewing historical contracts

## Relationship Rules

Contract must reference Vehicle:

```csharp
public int VehicleId { get; set; }
public Vehicle Vehicle { get; set; } = null!;
```

Vehicle may have multiple contracts historically.

Use:

```csharp
.OnDelete(DeleteBehavior.Restrict)
```

for all critical relationships.

## Migration Safety Rules

Never perform destructive migrations directly.

Migration strategy must be:

1. Add nullable columns first
2. Backfill data
3. Verify integrity
4. Then enforce constraints

Never drop old columns before migration verification.

## UI Rules For Vehicle Workflow

Vehicle selection and creation should use:

- lookup dropdowns
- searchable vehicle forms
- reusable vehicle workflows

Contract views should display snapshot data only.

## Marketplace Readiness

Future system modules may include:

- vehicle marketplace
- ownership history
- vehicle listings
- vehicle inspections
- installment sales
- online contracts
- QR verification
- public vehicle pages

Architecture decisions must preserve future scalability without prematurely implementing these modules.

---

# AI AGENT EXECUTION RULES

AI agents and automated tools must follow this file and the shared Engineering Standards.

Work authorization levels:

- **A — Read and analyze:** inspection, explanation, and recommendations; no file changes.
- **B — Local implementation:** focused local edits only within the explicitly approved scope.
- **C — Commit and push:** requires explicit authorization for the relevant commit and push operation.
- **D — High impact:** production changes, production data migrations, infrastructure changes, destructive actions, and releases require explicit approval for each consequential operation.

Before making architectural changes:

1. Analyze:
   - DbContext
   - Models
   - Controllers
   - Views
   - Services
   - Existing migrations
   - Relevant tests

2. Explain impact before destructive changes.
3. Prefer incremental refactoring over rewrites.
4. Protect existing local and untracked user changes.
5. Review the complete diff and stage only approved files.
6. Run relevant verification and report warnings, errors, and limitations accurately.

Never:

- remove data blindly
- recreate databases unnecessarily
- drop production columns without migration strategy
- force-push or rewrite shared Git history
- claim tests, builds, commits, or deployments succeeded without verification

All generated code must:

- compile successfully
- pass relevant tests
- preserve data and existing behavior
- enforce server-side authorization
- avoid exposing secrets or sensitive data

Always prioritize:

- stability and data integrity
- security and privacy
- contract and financial correctness
- maintainability
- performance and scalability
