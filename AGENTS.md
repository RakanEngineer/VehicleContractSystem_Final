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

