# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

WorkforceManager (نظام إدارة إنتاجية وأجور العمال) is a WPF desktop app for managing factory workers, their
skills, products and their manufacturing stages, daily piece-production entry (with automatic "workday"
calculation), attendance, and performance evaluation vs. team average. All in-code comments and docs are
written in Arabic — follow that convention when editing existing files.

The solution root is `WorkforceManager/` (contains `WorkforceManager.sln`), one level below the repo root.

## Commands

Run from inside the `WorkforceManager/` folder (where the `.sln` lives):

```bash
dotnet restore

# one-time global tool needed for migrations
dotnet tool install --global dotnet-ef

# create/update a migration after changing entities in WorkforceManager.Core/Models or AppDbContext
dotnet ef migrations add <MigrationName> --project WorkforceManager.Data --startup-project WorkforceManager.UI

# run the app (auto-creates + seeds the SQLite DB on first run)
dotnet run --project WorkforceManager.UI

# build / restore only
dotnet build
```

There is no test project in the solution yet.

The SQLite DB lives outside the repo at `%LocalAppData%\WorkforceManager\workforce.db`, created by
`App.OnStartup` via `EnsureCreatedAsync` + `DatabaseSeeder.SeedIfEmptyAsync` — not via migrations at runtime.

## Architecture

Simplified Clean Architecture across 4 projects, each with its own `.csproj`, referenced in one direction only:

```
Core  <-  Data  <-  Business  <-  UI
Core  <-------------Business
Core  <----------------------- UI
```

- **WorkforceManager.Core** — POCO models (`Models/`), enums (`Enums/`), and repository interfaces
  (`Interfaces/`). Zero dependency on EF Core or WPF — this is what would let SQLite be swapped for
  SQL Server later without touching models or business logic.
- **WorkforceManager.Data** — EF Core + SQLite. `AppDbContext` is the single point of contact with the
  database (all relationships/cascade rules configured in `OnModelCreating`); `Repositories/` implement
  the Core interfaces; nothing outside this project talks to `AppDbContext` directly.
- **WorkforceManager.Business** — all business rules live here, nowhere else (especially not in UI code):
  `WorkdayCalculationService`, `PerformanceEvaluationService`, `AttendanceService`, `WorkerProfileService`,
  plus their DTOs in `DTOs/`.
- **WorkforceManager.UI** — WPF, MVVM (CommunityToolkit.Mvvm) + MaterialDesignThemes. `App.xaml.cs` wires
  up DI via `Microsoft.Extensions.Hosting`'s `Host` (`AppHost`) — this is the single place new
  repositories/services/views get registered. `Views/` currently only has a README describing the planned
  screens (WorkersView, ProductsView, DailyEntryView, ReportsView) — actual view/viewmodel implementation
  is still pending.

### Domain model relationships

- `Product` 1—* `ProductionStage` (cascade delete): each stage carries its own `PiecesPerWorkday` ("كوتة
  اليومية") — the same stage name can repeat across products with an independent quota/price each.
- `Worker` *—* `ProductionStage` via `WorkerSkill` (join entity, unique per worker+stage): which stages a
  worker is qualified to perform.
- `DailyProduction`: one entry = pieces produced by one worker on one stage on one date. Snapshots
  `PiecesPerWorkdayAtEntry` from the stage at insert time (not read live) so historical records stay
  correct even if a stage's quota changes later. `WorkdaysCompleted` is a `[NotMapped]` computed property
  (`PieceCount / PiecesPerWorkdayAtEntry`). Delete of `Worker`/`ProductionStage` is `Restrict` here to
  protect historical records.
- `Attendance`: one row per worker per date (unique index), independent of `DailyProduction` — a worker can
  be present with no production logged, but absence implies no production. Cascade-deletes with `Worker`.
- Soft-delete convention: `Worker.IsActive` / `Product.IsActive` / `ProductionStage.IsActive` flags are used
  instead of hard deletes, to preserve historical production/attendance records.

### Business logic notes

- `WorkdayCalculationService.RecordProductionAsync` is the only place a `DailyProduction` row should be
  created — it snapshots the stage quota automatically.
- `PerformanceEvaluationService.EvaluateDayAsync` ranks workers for a given date against the average
  workdays of only the workers who actually produced that day (absentees aren't averaged in at zero).
  Unexcused absence (`AbsentWithoutPermission`) always ranks worst regardless of any production; excused
  absence is neutral (`Average`). Thresholds (`TopPerformerThreshold`, `AboveAverageThreshold`,
  `BelowAverageThreshold`) are relative percentages vs. team average, defined as constants in that service.
- `AttendanceService.RecordAttendanceAsync` is an upsert (one record per worker/date).
