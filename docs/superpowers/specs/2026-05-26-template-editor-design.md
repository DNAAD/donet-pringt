# Template Editor Design

## Goal

Add a safe first version of online label template editing for dotnet-print. Users can adjust existing template elements only: X position, Y position, font size, and bold style.

## Scope

- Supported templates: default 80mm x 30mm label and silver 80mm x 30mm label.
- Supported editable fields: existing text and QR/native drawing commands that have stable element keys.
- Supported properties:
  - `x`
  - `y`
  - `fontSizePt`
  - `bold`
- Unsupported in v1:
  - adding elements
  - deleting elements
  - editing text content
  - changing width or height
  - changing font family
  - italic/underline
  - drag-and-drop
  - arbitrary rotation changes

## Architecture

The code keeps built-in templates as the source of safe defaults. User edits are saved as local overrides in `settings.json` under `templateOverrides`. During preview and printing, the native planner generates the built-in drawing plan, then applies overrides by element key. This keeps `/preview/gdi.png` and `/print/tag` on the same GDI rendering path.

## Data Model

`PrintClientSettings` gains:

```json
{
  "templateOverrides": {
    "default": {
      "productName": {
        "x": 10.2,
        "y": 0,
        "fontSizePt": 4.6,
        "bold": false
      }
    },
    "silver": {}
  }
}
```

Overrides are optional. Missing values fall back to built-in command values.

## Element Keys

Stable command element keys are added to `NativeDrawCommand`:

- `identifier`
- `qualityMark`
- `qrCode`
- `qrNote`
- `productName`
- `standardText`
- `addressText`
- `salesCode`
- `finishedWeightLabel`
- `finishedWeightValue`
- `roughWeightLabel`
- `roughWeightValue`
- `additionalPrice`
- `partRow`
- `footerText`
- `verticalIdentifier`
- silver-specific keys reuse the same logical names where possible.

For repeated part rows, v1 allows one shared `partRow` override applied to all part row commands.

## UI

`/settings-ui` remains the entry page. It adds a "template editor" panel below the current printer/settings controls:

- Template selector continues to choose default or silver.
- Element selector lists editable existing elements for the selected template.
- Inputs: X, Y, font size, bold.
- Save button persists settings.
- Restore controls are not required for v1; clearing an input can be added later, but the initial UI always posts explicit values for the selected element.

## Error Handling

- Invalid numeric values are ignored and existing saved values remain.
- Unknown template or element keys are ignored.
- Empty settings file still loads safe defaults.

## Testing

Tests cover:

- Settings persistence round-trips template overrides.
- Native planner applies X/Y/font size/bold overrides to default template commands.
- Native planner applies overrides to silver template commands.
- Settings page renders the template editor fields and selected override values.

## Acceptance Criteria

- Users can edit X/Y/font size/bold for an existing element from `/settings-ui`.
- Saved overrides affect `/preview/gdi.png`, `/preview/native-plan`, test print, and `/print/tag`.
- Existing label orientation remains Direct80x30.
- Existing CORS/PNA behavior is unchanged.
