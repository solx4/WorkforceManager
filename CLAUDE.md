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
  repositories/services/views get registered. `WorkersView` (+ `WorkersViewModel`, `WorkerEditDialog`) is
  implemented: workers grid with current-week stats, unified name/skill search, profile panel with skills
  management and weekly history, add/edit/soft-delete. `DailyEntryView` is implemented: one shared date +
  3 tabs — batch production entry per stage (qualified workers, falls back to all actives when no skills
  are linked yet), attendance grid (upsert per worker/date), and penalties (add with reason/deduction,
  list + delete for the day). `ReportsView` is implemented: daily evaluation tab (colored ratings vs team
  average) + weekly sheet tab (net-workdays ranking, week navigation, Excel export via
  `WeeklyReportExcelService`/ClosedXML in Business). Only ProductsView is still a placeholder in
  `MainWindow`. Navigation uses `Checked` (not `Click`) on the sidebar radios — handlers guard against
  the initial `Checked` that fires during `InitializeComponent` before `MainContent` exists. ViewModels take `IServiceScopeFactory` and create a scope per operation
  (keeps DbContext short-lived). Gotcha: WPF implicit styles don't apply to derived types, so the
  `TargetType="Window"` style in App.xaml does NOT hit `MainWindow` — set `FlowDirection="RightToLeft"`
  explicitly on each window.

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
- `Penalty`: a disciplinary penalty on a worker on a date (reason + `PenaltyDeduction` enum: HalfDay=0.5,
  OneDay=1, ThreeDays=3, OneWeek=6 workdays — a work week is 6 days since Friday is off). Independent of
  attendance (can be issued while present). Cascade-deletes with `Worker`. Deleting a wrongly-entered
  penalty is a hard delete (no soft-delete value).
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
- `WeeklySummaryService` is the heart of weekly math. The work week runs **Thursday → Wednesday**
  (`GetWorkWeekRange`). Weekly counters are computed on the fly from `DailyProduction`/`Attendance`/
  `Penalty` records — nothing weekly is stored, so "a new week starts fresh" while all history stays
  queryable. Net workdays = produced − unexcused-absence deduction (**0.5 workday per
  `AbsentWithoutPermission` day**; excused absence costs nothing) − penalty deductions. Best worker of
  the week = highest net, only if they produced and net > 0.

## Environment note

.NET 8 SDK was installed via winget but may not be in PATH for fresh shells; if `dotnet` isn't found in
PowerShell, prepend `$env:PATH = [System.Environment]::GetEnvironmentVariable("PATH","Machine") + ";" +
[System.Environment]::GetEnvironmentVariable("PATH","User")`.
