# Settings UI Layout Refresh Design

## Goal

Improve `http://127.0.0.1:37122/settings-ui` so the page is easier to use for printer setup, label preview, and template element adjustment, without changing print behavior or request contracts.

## Scope

- Target project: `D:\sales_system\dotnet-print`
- Main file: `src/Zytxt.PrintClient.Host/Settings/SettingsPageRenderer.cs`
- Tests: `tests/Zytxt.PrintClient.Tests/Program.cs`
- Keep all existing form field names, submit actions, preview URLs, and diagnostic links compatible.
- Do not change `/print/tag`, GDI drawing logic, printer selection persistence, or template override semantics.

## Design

Use a three-column settings workbench on desktop:

- Left column: printer connection, CORS origin list, global X/Y offset, save and test actions.
- Center column: large GDI preview, current paper/offset/template metrics, direct links to diagnostic preview pages.
- Right column: existing template element editor for X/Y/font size/bold, with the same supported fields as the current POC.

On narrow screens the columns collapse into a single column. The visual language should be a quiet operations UI: light gray page background, white panels, clear section titles, compact controls, and grouped action rows.

## Acceptance Criteria

- The settings page exposes distinct layout classes for the shell, left column, central preview, and template sidebar.
- Existing tests for printer selection, offsets, CORS, template switching, test print actions, and template override fields remain passing.
- New layout test verifies the key sections exist.
- `dotnet run` test project, `dotnet build` solution, and Harness validation pass.
