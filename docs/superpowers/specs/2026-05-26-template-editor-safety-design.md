# Template Editor Safety Design

## Goal

Make the `/settings-ui` template editor safer and easier to tune by adding UI-only preview zoom, current-element reset, and numeric range validation.

## Design

- Preview zoom is a range input named `previewZoom` from `1` to `2` with `0.25` steps. JavaScript scales only the on-page `.preview` image.
- Template element reset is a submit button named `templateReset` with value `current`. The server still saves printer/CORS/offset fields, then removes the selected template element override.
- Template element inputs use these ranges: X `0..80`, Y `0..30`, font size `1..12`. The server applies the same clamp so forged forms cannot store unsafe values.
- Existing print, preview PNG, and `/print/tag` behavior are unchanged.
