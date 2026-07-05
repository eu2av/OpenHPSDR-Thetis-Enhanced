# OpenHPSDR-Thetis — Enhanced Fork by Yurij-eu2av

> **Base:** Thetis 2.10.x (VS2026 solution)  
> **Author:** Yurij-eu2av  
> **Purpose:** Local enhancements for openHPSDR transceivers (Anvelina PRO3 / ANAN7000D / Hermes-Lite 2 compatible), documented for upstream integration.

---

## Overview

This fork contains a set of focused quality-of-life and accuracy improvements for the **Thetis SDR** client software. All changes are **opt-in by default** (zero regressions) and do not alter existing behaviour unless the user explicitly enables them.

The modifications fall into four main areas:

1. **Modern waterfall rendering** — 16-bit float pipeline, new colour schemes, quality levels
2. **Display quality controls** — Custom-scheme LUT expansion, Bayer dithering, gamma curve
3. **HiDPI/2K support** — Per-monitor DPI awareness toggle
4. **Calibration improvements** — Per-band power detector calibration, voltage calibration

---

## Key Features

### 1. 16-bit Float Waterfall Pipeline + DeviceContext Migration

| Feature | Description |
|---|---|
| **Direct2D 1.1 DeviceContext** | Migrated from legacy `ID2D1RenderTarget` to `ID2D1DeviceContext` (Windows 8+). Enables 10/16-bit surface formats and improves resilience to GPU driver resets. |
| **16-bit Half-Float Swap Chain** | Selectable `R16G16B16A16_Float` (65 536 levels) vs legacy `B8G8R8A8_UNorm` (256 levels). Live switchable without restart. |
| **sRGB→Linear Compensation** | Correct colour appearance across 8-bit and 16-bit modes. Handles stroke width scaling, alpha blending, and background photo conversion. |
| **3 New Colour Schemes** | `Console 256`, `Thermal 256`, `DeepBlue 256` — multi-stop gradient palettes (256-step LUT) replacing hard-coded if/else bands. |
| **Quality Levels** | `Classic` / `Vivid` (+30% sat) / `Sharp` (+contrast S-curve) / `Ultra` (+dither). S-curve applies before colour lookup, works on all schemes. |
| **Bug fixes** | Fixed long-standing dim-waterfall-after-Custom bug and ADC overload flicker on TX→RX transition. |

**New files:** `WaterfallPixelWriter.cs`, `WaterfallPalette.cs`  
**Modified:** `display.cs`, `WaterfallEnhancer.cs`, `enums.cs`, `setup.cs`, `console.cs`, `Thetis.csproj`

---

### 2. Waterfall Quality Enhancement (Custom Scheme)

Expands the legacy 101-step LUT in the Custom colour scheme to user-selectable 256/512/1024 steps, with optional Bayer 8×8 ordered dithering and adjustable gamma curve.

| Control | Range | Default | Effect |
|---|---|---|---|
| Palette Resolution | 101 / 256 / 512 / 1024 | 101 | Smoother gradients, less banding |
| Dither | ON/OFF | OFF | Masks banding even at 101 steps |
| Gamma | 0.50 – 2.00 | 1.00 | Stretch weak signals or boost strong-signal contrast |

**New file:** `WaterfallEnhancer.cs`  
**Modified:** `display.cs`, `setup.cs`

---

### 3. Per-Monitor DPI Awareness (HiDPI / 2K Displays)

Adds a registry-backed toggle (`HKCU\Software\OpenHPSDR\Thetis-x64\DpiAwareness`) that calls `SetProcessDpiAwarenessContext(PER_MONITOR_V2)` at process startup. Makes waterfall and panadapter render at native physical resolution on HiDPI monitors instead of being stretched by DWM.

- **Default:** OFF (identical to stock Thetis)
- **Requires restart** after toggling
- **Safe rollback:** uncheck → restart → back to legacy behaviour

**Modified:** `clsProgressLog.cs`, `console.cs`, `setup.cs`

---

### 4. Per-band Power Detector Calibration

Compensates frequency-dependent coupler response (tandem-match) that causes 4–12 % power-reading errors on low bands (160/80/40 m). A per-band multiplier `k` is applied equally to FWD and REV, so **SWR remains unchanged**.

| Band | Default `k` (Anvelina PRO3 / ANAN7000D) |
|---|---|
| 160 m | 0.91 |
| 80 m | 0.92 |
| 40 m | 0.96 |
| 60 m – 6 m | 1.00 (no correction) |

- **Default:** 1.00 on all bands (zero regressions)
- **Factory presets** auto-load for ANVELINAPRO3 / ANAN7000D models
- **"Reset to Defaults"** button restores factory calibration

**Modified:** `console.cs`, `setup.cs`, `setup.designer.cs`

---

### 5. Voltage Calibration (PA Volts + Supply 13.8 V)

Adds per-device multipliers `PAVoltCal` (AIN3) and `SupplyVoltCal` (AIN6) to correct hard-coded divider ratios after FPGA timing changes.

- **Range:** 0.100 – 5.000, step 0.001, default 1.000
- **"V Default"** button resets both to 1.000

**Modified:** `console.cs`, `setup.cs`

---

## Safety Summary

| Aspect | Policy |
|---|---|
| **Defaults** | All new features are OFF or set to neutral (1.00 / Classic / 8-bit). |
| **Existing schemes** | Enhanced, SPECTRAN, LinRad, etc. remain untouched. |
| **FPGA / native code** | Not modified (except reading ADC overload flag). |
| **Fallbacks** | Legacy RenderTarget fallback if DeviceContext fails; 8-bit fallback if 16-bit errors. |
| **Persistence** | All settings saved via existing `SaveOptions()/getOptions()` → `database.xml`. |
| **Backups** | Original files preserved with date-stamped extensions (`.detcal_backup_20260702`, etc.). |

---

## Building

Requires **Visual Studio 2022** (or VS2026 solution) with .NET Framework / .NET Desktop workload.

```bash
# Open solution
"Project Files\Source\Thetis.sln"

# Build platform: x64
# Target framework: .NET (as configured in project)
```

No additional dependencies beyond stock Thetis.

---

## Full Changelog

See [`CHANGELOG_Yurij-eu2av_EN.md`](CHANGELOG_Yurij-eu2av_EN.md) for day-by-day technical notes, file change lists, line-number references, and calibration procedures.

---

## Upstream Integration Notes

These changes are structured as **additive, non-breaking patches** intended for submission to the main Thetis authors. Each feature:

- Lives in its own logical commit boundary (can be cherry-picked individually)
- Uses existing Thetis persistence/UI patterns
- Maintains backward compatibility
- Includes inline comments and backup annotations

---

## License

This fork inherits the same dual-licensing terms as upstream [OpenHPSDR-Thetis](https://github.com/TAPR/OpenHPSDR-Thetis). See `LICENSE` and `LICENSE-DUAL-LICENSING` in the repository root.

---

*73, Yurij-eu2av*
