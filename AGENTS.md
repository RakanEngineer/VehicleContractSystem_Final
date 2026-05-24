# Vehicle Contract Management System

ASP.NET Core MVC + Entity Framework Core + SQL Server

---

# PROJECT ARCHITECTURE

## Architecture Pattern

- ASP.NET Core MVC
- Entity Framework Core
- Identity Authentication
- Repository pattern is NOT used currently
- DbContext is used directly in controllers/services

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

---

## Models

Contain:

- entities
- relationships
- validation attributes

---

## ViewModels

Used ONLY for:

- UI forms
- dropdown lists
- combined page data

Never expose database entities directly to complex forms.

---

## Services

Contain:

- payment logic
- business rules
- calculations
- reusable workflows

---

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

---

## Decimal Fields

Always use:

```csharp
[Column(TypeName = "decimal(18,2)")]
```

for money values.

---

## Computed Properties

Use read-only expressions:

```csharp
public decimal RemainingAmount => VehiclePrice - PaidAmount;
```

Never assign values manually to computed properties.

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

---

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

---

## Validation

Always include:

```html
<span asp-validation-for=""></span>
```

---

# CODING STANDARDS

## Naming

### Classes

PascalCase

Examples:

- ContractController
- VehiclePaymentService

---

### Methods

PascalCase

---

### Private Fields

_prefixCamelCase

Example:

```csharp
private readonly ApplicationDbContext _context;
```

---

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

---

# UI RULES

- Arabic text only
- RTL only
- Consistent Bootstrap styling
- Use cards and responsive layout
- Use validation summaries

---

# IMPORTANT PROJECT RULES

Before modifying code:

1. Understand dependencies
2. Search related models/controllers/views
3. Preserve existing functionality
4. Avoid breaking migrations
5. Avoid duplicate code

Never:

- create hacky fixes
- duplicate logic
- ignore nullable warnings
- remove relationships blindly

---

# MIGRATION RULES

Before creating migrations:

- verify relationships
- verify FK delete behaviors
- verify seed data IDs

After migration:

- Build
- Add-Migration
- Update-Database

must all succeed without errors.

---

# GOAL

Maintain:

- clean architecture
- stable database
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

---

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

---

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

---

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

---

## Migration Safety Rules

Never perform destructive migrations directly.

Migration strategy must be:

1. Add nullable columns first
2. Backfill data
3. Verify integrity
4. Then enforce constraints

Never drop old columns before migration verification.

---

## UI Rules For Vehicle Workflow

Vehicle selection and creation should use:
- lookup dropdowns
- searchable vehicle forms
- reusable vehicle workflows

Contract views should display snapshot data only.

---

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

Architecture decisions must preserve future scalability.

# AI AGENT EXECUTION RULES

Before making architectural changes:

1. Analyze:
   - DbContext
   - Models
   - Controllers
   - Views
   - Existing migrations

2. Explain impact before destructive changes.

3. Prefer incremental refactoring over rewrites.

4. Never:
   - remove data blindly
   - recreate databases unnecessarily
   - drop production columns without migration strategy

5. All generated code must:
   - compile successfully
   - pass migrations
   - preserve existing data

6. Always prioritize:
   - stability
   - maintainability
   - future scalability