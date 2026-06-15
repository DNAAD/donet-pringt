# Template Editor Safety Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add preview zoom, current element reset, and range validation to the dotnet-print settings template editor.

**Architecture:** Keep rendering in `SettingsPageRenderer`; extract form mutation into `SettingsFormApplier` so reset/clamp behavior is unit-testable. Program endpoints continue to load settings, apply forms, save settings, then redirect/print as before.

**Tech Stack:** .NET 8, ASP.NET minimal host, server-rendered HTML/CSS/JavaScript, existing custom test runner.

---

### Task 1: Tests

**Files:**
- Modify: `D:\sales_system\dotnet-print\tests\Zytxt.PrintClient.Tests\Program.cs`

- [ ] Add renderer assertions for `name="previewZoom"`, preview zoom label, `name="templateReset"`, and template input min/max attributes.
- [ ] Add form applier assertions for clamping X/Y/font size.
- [ ] Add form applier assertions for removing the current template element override.
- [ ] Run the test command and confirm the new tests fail before implementation.

### Task 2: Form Applier

**Files:**
- Create: `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Host\Settings\SettingsFormApplier.cs`
- Modify: `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Host\Program.cs`

- [ ] Implement `SettingsFormApplier.Apply(PrintClientSettings settings, IFormCollection form, string previewTemplate)`.
- [ ] Clamp template element X to `0..80`, Y to `0..30`, and font size to `1..12`.
- [ ] Remove the selected element override when `templateReset=current`.
- [ ] Replace Program's local form apply helpers with the new helper.

### Task 3: Renderer UI

**Files:**
- Modify: `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Host\Settings\SettingsPageRenderer.cs`

- [ ] Add preview zoom control near the GDI preview toolbar.
- [ ] Add template reset button.
- [ ] Add min/max attributes to template X/Y/font size inputs.
- [ ] Add client-side JavaScript to scale the preview image and display the percentage.

### Task 4: Verification

- [ ] Run `dotnet run --project D:\sales_system\dotnet-print\tests\Zytxt.PrintClient.Tests\Zytxt.PrintClient.Tests.csproj`.
- [ ] Run `dotnet build D:\sales_system\dotnet-print\zytxt-dotnet-print.sln`.
- [ ] Run `powershell -ExecutionPolicy Bypass -File D:\sales_system\.harness\tools\validate-harness.ps1`.
- [ ] Browser-check `http://127.0.0.1:37122/settings-ui`.
