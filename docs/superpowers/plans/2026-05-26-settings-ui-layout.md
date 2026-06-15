# Settings UI Layout Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refresh the local print helper settings UI into a clearer three-column workbench while preserving existing print and settings behavior.

**Architecture:** Keep `SettingsPageRenderer` as the single HTML renderer. Reorganize the generated markup and CSS only, and use tests to assert stable sections plus existing field/action compatibility.

**Tech Stack:** .NET 8, ASP.NET minimal host, server-rendered HTML/CSS, existing custom test runner.

---

### Task 1: Layout Test

**Files:**
- Modify: `D:\sales_system\dotnet-print\tests\Zytxt.PrintClient.Tests\Program.cs`

- [x] **Step 1: Write the failing test**

Add assertions to `TestSettingsPageRendererCalibrationWorkbench` for `settings-shell`, `settings-column settings-column-left`, `preview-stage`, `template-sidebar`, `打印机与连接`, and `校准与测试`.

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
dotnet run --project D:\sales_system\dotnet-print\tests\Zytxt.PrintClient.Tests\Zytxt.PrintClient.Tests.csproj
```

Expected: FAIL because the old renderer does not contain the new layout classes and section titles.

### Task 2: Settings Renderer Layout

**Files:**
- Modify: `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Host\Settings\SettingsPageRenderer.cs`

- [ ] **Step 1: Implement the three-column workbench**

Update the returned HTML/CSS so the page contains:

- `settings-shell` as the outer grid.
- `settings-column settings-column-left` for printer, CORS, offset, and actions.
- `preview-stage` for GDI preview and metrics.
- `template-sidebar` for existing template element editor output.

- [ ] **Step 2: Preserve existing contracts**

Keep all existing `name=...`, `formaction=...`, `href=...`, and preview image URL behavior.

- [ ] **Step 3: Run tests**

Run the same test command and expect all tests to pass.

### Task 3: Verification And Harness

**Files:**
- Update: `D:\sales_system\.harness\changes\improve-dotnet-print-settings-ui-layout-20260526\summary.md`
- Update reports under that change directory.

- [ ] **Step 1: Run build**

```powershell
dotnet build D:\sales_system\dotnet-print\zytxt-dotnet-print.sln
```

- [ ] **Step 2: Validate Harness**

```powershell
powershell -ExecutionPolicy Bypass -File D:\sales_system\.harness\tools\validate-harness.ps1
```

- [ ] **Step 3: Check settings page**

Start the host, open or request `/settings-ui`, confirm HTTP 200 and the refreshed layout markup is present, then stop the host.
