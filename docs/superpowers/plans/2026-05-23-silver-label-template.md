# Silver Label Template Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add .NET/GDI template selection so factory `25003` prints with a silver 80x30 layout and other labels keep the default layout.

**Architecture:** `LabelRenderPlanner` maps incoming `factoryNo` to a template key on `LabelRenderPlan`. `NativeLabelDrawingPlanner` remains the public entry point and delegates silver plans to a silver-specific native layout planner while preserving the existing default layout.

**Tech Stack:** .NET 8, System.Drawing/GDI, existing custom test runner.

---

### Task 1: Template Selection Model

**Files:**
- Modify: `src/Zytxt.PrintClient.Core/Labels/LabelItem.cs`
- Modify: `src/Zytxt.PrintClient.Core/Labels/LabelRenderPlan.cs`
- Modify: `src/Zytxt.PrintClient.Core/Labels/LabelRenderPlanner.cs`
- Test: `tests/Zytxt.PrintClient.Tests/Program.cs`

- [ ] Add failing tests for `factoryNo == 25003` and default template behavior.
- [ ] Add `LabelTemplateKey` enum with `Default80x30` and `Silver80x30`.
- [ ] Add `FactoryNo` input to `LabelItem` and set `TemplateKey` in `LabelRenderPlan`.
- [ ] Run the targeted test command and confirm the new tests pass.

### Task 2: Silver Native Drawing Planner

**Files:**
- Modify: `src/Zytxt.PrintClient.Core/NativeDrawing/NativeLabelDrawingPlanner.cs`
- Create: `src/Zytxt.PrintClient.Core/NativeDrawing/SilverNativeLabelDrawingPlanner.cs`
- Test: `tests/Zytxt.PrintClient.Tests/Program.cs`

- [ ] Add a failing test asserting silver native commands include `总重(g)`, price text, and no address text.
- [ ] Implement `SilverNativeLabelDrawingPlanner` using Electron `Label80x30Silver.vue` as the field reference.
- [ ] Delegate from `NativeLabelDrawingPlanner.CreatePlan` when `TemplateKey` is `Silver80x30`.
- [ ] Run the targeted test command and confirm it passes.

### Task 3: Preview Sample And Renderers

**Files:**
- Modify: `src/Zytxt.PrintClient.Host/Program.cs`
- Modify: `src/Zytxt.PrintClient.Core/Labels/LabelPreviewHtmlRenderer.cs`
- Test: `tests/Zytxt.PrintClient.Tests/Program.cs`

- [ ] Add failing assertions for silver HTML class or content differences if needed.
- [ ] Set the preview sample `FactoryNo` to `25003` so `/preview/gdi.png` exercises the silver template.
- [ ] Keep default HTML preview compatible with existing tests.

### Task 4: Verification

**Commands:**
- `dotnet run --project D:\sales_system\dotnet-print\tests\Zytxt.PrintClient.Tests\Zytxt.PrintClient.Tests.csproj`
- `dotnet build D:\sales_system\dotnet-print\zytxt-dotnet-print.sln`
- `powershell -ExecutionPolicy Bypass -File D:\sales_system\.harness\tools\validate-harness.ps1`

- [ ] Record command outcomes in `.harness/changes/feature-dotnet-print-silver-label-20260523/summary.md`.
