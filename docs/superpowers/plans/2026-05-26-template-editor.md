# Template Editor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add v1 online template editing for existing label elements: X, Y, font size, and bold.

**Architecture:** Keep built-in native drawing plans as defaults, add stable command element keys, and apply optional saved overrides from `PrintClientSettings.TemplateOverrides`. Settings UI posts one selected element override at a time; preview and real print use the same overridden native plan.

**Tech Stack:** .NET 8, System.Drawing/GDI, ASP.NET Core minimal API, JSON settings, existing custom console test runner.

---

### File Structure

- Modify `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Core\NativeDrawing\NativeDrawCommand.cs` to add an optional `ElementKey`.
- Create `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Core\Settings\TemplateElementOverride.cs` for nullable override properties.
- Modify `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Core\Settings\PrintClientSettings.cs` to add `TemplateOverrides`.
- Modify `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Core\NativeDrawing\NativeLabelDrawingPlanner.cs` and `SilverNativeLabelDrawingPlanner.cs` to assign element keys and apply overrides.
- Modify `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Host\Program.cs` to pass overrides and bind template editor form fields.
- Modify `D:\sales_system\dotnet-print\src\Zytxt.PrintClient.Host\Settings\SettingsPageRenderer.cs` to render the editor controls.
- Modify `D:\sales_system\dotnet-print\tests\Zytxt.PrintClient.Tests\Program.cs` to add regression tests.

### Task 1: Settings Model and Persistence

**Files:**
- Create: `src/Zytxt.PrintClient.Core/Settings/TemplateElementOverride.cs`
- Modify: `src/Zytxt.PrintClient.Core/Settings/PrintClientSettings.cs`
- Test: `tests/Zytxt.PrintClient.Tests/Program.cs`

- [ ] **Step 1: Write failing settings persistence test**

Add a test that saves `TemplateOverrides["default"]["productName"]` with `X`, `Y`, `FontSizePt`, and `Bold`, then reloads settings and asserts the values round-trip.

- [ ] **Step 2: Run test and verify RED**

Run:

```powershell
dotnet run --project D:\sales_system\dotnet-print\tests\Zytxt.PrintClient.Tests\Zytxt.PrintClient.Tests.csproj
```

Expected: compile failure because `TemplateElementOverride` or `TemplateOverrides` is missing.

- [ ] **Step 3: Implement model**

Create `TemplateElementOverride` with nullable decimal/bool properties and add:

```csharp
public Dictionary<string, Dictionary<string, TemplateElementOverride>> TemplateOverrides { get; set; } = [];
```

- [ ] **Step 4: Run test and verify GREEN**

Run the same test command. Expected: persistence test passes or next planned test failure appears.

### Task 2: Native Drawing Override Application

**Files:**
- Modify: `src/Zytxt.PrintClient.Core/NativeDrawing/NativeDrawCommand.cs`
- Modify: `src/Zytxt.PrintClient.Core/NativeDrawing/NativeLabelDrawingPlanner.cs`
- Modify: `src/Zytxt.PrintClient.Core/NativeDrawing/SilverNativeLabelDrawingPlanner.cs`
- Test: `tests/Zytxt.PrintClient.Tests/Program.cs`

- [ ] **Step 1: Write failing planner tests**

Add tests that create overrides for `productName` and `roughWeightValue`, then assert `X`, `Y`, `FontSizePt`, and `Bold` are applied to the corresponding native commands for default and silver templates.

- [ ] **Step 2: Run test and verify RED**

Run the test project. Expected: compile failure or assertion failure because planner does not accept overrides yet.

- [ ] **Step 3: Add `ElementKey` and override logic**

Add optional `ElementKey` to `NativeDrawCommand`. Update helper `Text`/`QrCode` methods to accept element keys. Add planner overload:

```csharp
CreatePlan(LabelRenderPlan labelPlan, LabelOffset? offset, IReadOnlyDictionary<string, TemplateElementOverride>? overrides)
```

Apply overrides after command creation by copying commands with updated X/Y/font size/bold when a matching element key exists.

- [ ] **Step 4: Update call sites**

Existing `CreatePlan(labelPlan, offset)` keeps working by passing no overrides. Host call sites later pass selected template overrides.

- [ ] **Step 5: Run test and verify GREEN**

Run the test project. Expected: default and silver override tests pass.

### Task 3: Settings UI Form Binding and Rendering

**Files:**
- Modify: `src/Zytxt.PrintClient.Host/Program.cs`
- Modify: `src/Zytxt.PrintClient.Host/Settings/SettingsPageRenderer.cs`
- Test: `tests/Zytxt.PrintClient.Tests/Program.cs`

- [ ] **Step 1: Write failing UI test**

Add a test that renders `/settings-ui` for a settings object with `templateOverrides.default.productName`, then asserts the HTML contains `name="templateElementKey"`, `value="productName"`, `name="templateX"`, `name="templateY"`, `name="templateFontSizePt"`, and `name="templateBold"`.

- [ ] **Step 2: Run test and verify RED**

Run the test project. Expected: assertion failure because fields are not rendered.

- [ ] **Step 3: Render editor controls**

Add a compact template editor section under existing settings controls with a template element select and inputs for X/Y/font size/bold. Render current selected element values from overrides or built-in defaults.

- [ ] **Step 4: Bind form values**

In `ApplySettingsForm`, read `templateElementKey`, `templateX`, `templateY`, `templateFontSizePt`, and `templateBold`, then upsert the override under the selected preview template.

- [ ] **Step 5: Run test and verify GREEN**

Run the test project. Expected: UI test passes.

### Task 4: Host Wiring and Full Verification

**Files:**
- Modify: `src/Zytxt.PrintClient.Host/Program.cs`
- Test: existing test project

- [ ] **Step 1: Pass overrides to all preview/print paths**

For `/settings-ui/test-print`, `/preview/native-plan`, `/preview/gdi.png`, and `/print/tag`, pass the saved override dictionary for the label template into `NativeLabelDrawingPlanner.CreatePlan`.

- [ ] **Step 2: Run full tests**

Run:

```powershell
dotnet run --project D:\sales_system\dotnet-print\tests\Zytxt.PrintClient.Tests\Zytxt.PrintClient.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 3: Run build**

Run:

```powershell
dotnet build D:\sales_system\dotnet-print\zytxt-dotnet-print.sln
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 4: Run Harness validation**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File D:\sales_system\.harness\tools\validate-harness.ps1
```

Expected: Harness validation passed.

### Self-Review

- Spec coverage: settings persistence, planner overrides, UI controls, preview/print wiring, and verification are covered.
- Placeholder scan: no TBD/TODO placeholders.
- Type consistency: `TemplateElementOverride`, `TemplateOverrides`, and `ElementKey` are consistently named.
