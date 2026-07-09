# CHANGELOG — Yurij-eu2av (Thetis modifications)

**Project:** Thetis SDR (openHPSDR transceiver software)  
**Base version:** Thetis 2.10.x (VS2026 solution)  
**Date:** 2026-07-09  
**Author:** Yurij-eu2av  
**Purpose:** Documenting local additions for submission to Thetis authors / upstream integration.

---

## ⚠️ Important note for users upgrading from previous builds

> **Russian / Русский:** При запуске этой версии **со старыми настройками / профилями** Thetis автоматически обнаружит несовместимую базу данных и предложит обновить её через диалог Yes/No. Перед обновлением будет создана резервная копия. Удерживать **Ctrl** при запуске больше не требуется — этот способ оставлен только как ручной force-upgrade на крайний случай.
>
> Из-за перехода на обработку **WDSP 2.0** настройки эквалайзера (EQ), скорее всего, придётся подстроить заново: высокие частоты теперь могут звучать громче, чем в предыдущих сборках.

> **English:** When launching this build **with old settings / profiles**, Thetis now automatically detects an incompatible database and offers to update it through a Yes/No dialog. A backup is created automatically before the update. Holding **Ctrl** at launch is no longer required — it remains only as a manual force-upgrade fallback.
>
> Because of the switch to **WDSP 2.0** DSP processing, equalizer (EQ) settings will likely need to be readjusted: high frequencies may now be louder than in previous builds.

---


## 2026-07-09 — Correct default values for CW APF (Audio Peaking Filter)

### Summary
- Investigated telegraphists' reports that the new APF modes (BI/DP/MA/GA) sound
  less pronounced than before.
- The filter algorithms in WDSP 2.00 are unchanged from WDSP 1.29; the difference
  was in Thetis defaults.
- The WDSP Guide recommends a default gain of **2.0** (6 dB) and bandwidth of
  **100 Hz**.
- Thetis was setting gain = 1.0 (0 dB) and bandwidth = 150 Hz, so the filter peak
  was barely audible.
- Defaults have been aligned with WDSP recommendations:
  - gain slider = 60 (6 dB);
  - bandwidth = 100 Hz.
- The gain label now shows units: "Gain: X dB".

### Files changed
- `Project Files/Source/Console/console.Designer.cs` — default `ptbCWAPFGain.Value = 60`,
  `ptbCWAPFBandwidth.Value = 100`.
- `Project Files/Source/Console/setup.designer.cs` — default 60/100 for APF gain/bandwidth
  trackbars on all RX paths.
- `Project Files/Source/Console/console.cs` — gain label now includes "dB".
- `Project Files/Source/Console/radio.cs` — default fields `rx_apf_gain`/`rx_apf_bw`
  updated to 2.0/100.

---

## 2026-07-09 — Fix for repeated “fft wisdom file is missing” warning

### Summary
- Fixed a bug where Thetis showed the “The fft wisdom file is missing...” message on every startup, even after wisdom had already been built successfully.
- The C# code was checking for `wdspWisdom00`, while WDSP has long created and used `wdspWisdom01`.
- The check now looks for `wdspWisdom01`. Any legacy `wdspWisdom00` left over from older versions is automatically renamed to `wdspWisdom01` (if `01` is missing) or deleted.
- The first run still builds wisdom and shows the warning; all subsequent runs proceed without the extra dialog.

### Files changed
- `Project Files/Source/Console/radio.cs` — check for `wdspWisdom01`, handle legacy `wdspWisdom00`.

---

## 2026-07-09 — Automatic database upgrade on schema mismatch

### Summary
- Added automatic detection of outdated or corrupted databases after adding new
  features/columns.
- On startup Thetis now validates database compatibility: all `TXProfile` columns
  must be present, no `DBNull` values in required fields, `VersionNumber`/`Version`
  must exist in `State`, and a `Default` row must exist in `TXProfileDef`.
- If the database is incompatible, a warning dialog asks for user consent to
  update. A backup is created automatically before any changes.
- The upgrade creates a new database and merges the old one using
  `ImportAndMergeDatabase`, filling missing fields with default values.
- Holding **Ctrl** at launch is no longer required — the consent dialog appears
  automatically. Ctrl (and an empty `updatedb.txt` file in the data folder) remain
  only as a manual force-upgrade fallback.

### Files changed
- `Project Files/Source/Console/clsDBMan.cs` — Yes/No consent dialog, pre-upgrade
  backup, passes schema-mismatch flag to `checkVersion()`.
- `Project Files/Source/Console/database.cs` — new `IsDatabaseCompatible()` method
  that validates the `TXProfile`/`TXProfileDef`/`State` schema and data.
- `Project Files/Source/Console/console.cs` — updated the fallback database error
  message to mention the automatic update and list Ctrl only as a manual fallback.

---

## 2026-07-09 — APF (CW) button: proper rendering and independent `SkinsAPF` folder

### Summary
- The APF type button (`btnAPF_type`) was changed from `ButtonTS` to `CheckBoxTS`
  with `Appearance = Button`, matching the `SEMI`/`chkQSK` button. It is now
  correctly handled by the skin engine and looks like a real button.
- The mode labels **DP / MA / GA / BI** are now readable: white bold centred text,
  flat style, no border, dark background.
- Layout adjusted: button size restored to 36×20 and the `Tune:` label moved left
  by ~1 mm (from `44, 16` to `40, 16`) so the gap between the button and the label
  is tidy.
- Added an **independent skin folder `SkinsAPF`** next to the main `Skins` folder.
  APF button images are loaded from there when the current skin does not provide
  them, so existing skins do not need to be updated.
- If the `SkinsAPF` folder is missing, Thetis **automatically creates it on first
  run** and populates it with default PNG stubs for all button states. Users can
  replace these PNGs with their own.

### Files changed
- `Project Files/Source/Console/console.Designer.cs` — `btnAPF_type` is now a
  `CheckBoxTS` with `Appearance = Button`, `AutoCheck = false`, flat style, white text.
- `Project Files/Source/Console/console.resx` — control type updated, obsolete
  `ButtonTS` properties removed, `lblCWAPFTune` position corrected.
- `Project Files/Source/Console/Skin.cs` — added fallback to the `SkinsAPF` folder
  (next to `Skins` and next to `Thetis.exe`), automatic creation of default PNGs
  when they are missing.
- `Project Files/Source/Console/Thetis.csproj` — MSBuild target copies `SkinsAPF/*.png`
  to the output directory during build.
- New `SkinsAPF/` folder at the repository root — default PNG stubs for the APF button.

---

## 2026-07-08 — WDSP 2.00 Phase Rotator (Auto/Reset/Asymmetry) and PSA over-drive indicator

### Summary
- Completed the Phase Rotator implementation per the WDSP 2.00 Guide:
  added the missing imports `SetTXAPHROTAutoMode`, `SetTXAPHROTAutoReset`,
  and `GetTXAPHROTAsymmetry`.
- Added to the **Phase Rotator** group on the DSP CFC tab:
  - **Auto FC** checkbox to enable/disable automatic corner-frequency optimisation;
  - **Reset** button to restart the optimiser from the default 338 Hz;
  - live **IN / OUT asymmetry** and current **FC** read-outs;
  - optimiser status label (**Off / Search / Done**).
- Added a PureSignal visual warning: when `GetPSInfo info[6] == 2`
  (severe over-drive per the WDSP 2.00 Guide), `lblPSInfo6` is highlighted in red.

### Files changed
- `Project Files/Source/Console/dsp.cs` — imports for `SetTXAPHROTAutoMode`,
  `SetTXAPHROTAutoReset`, `GetTXAPHROTAsymmetry`.
- `Project Files/Source/Console/setup.designer.cs` — Phase Rotator UI controls.
- `Project Files/Source/Console/setup.cs` — Auto/Reset handlers, asymmetry/status
  update timer (`timerPhRot_Tick`), persistence of `CFCPhaseRotatorAuto` in the TX profile.
- `Project Files/Source/Console/database.cs` — added `CFCPhaseRotatorAuto` column to the
  TX profile table, all default rows, and `VerifyTXProfileColumns()` migration for old DBs.
- `Project Files/Source/Console/PSForm.cs` — red highlight of `lblPSInfo6` when `info[6] == 2`.

### Fix (2026-07-08 later)
- The new Phase Rotator controls (Auto FC, Reset, status/asymmetry labels) were moved
  out of `setup.designer.cs` and are now created programmatically in `setup.cs` by
  `InitPhaseRotatorControls()`, following the same pattern as the Det. Cal. tab.
  This avoids the situation where the controls are visible in the designer but do not
  appear in the built runtime assembly.

---

## 2026-07-07 — PureSignal advanced controls restored for WDSP 2.00

### Summary
- Restored the WDSP 1.x exported functions `SetPSPtol`, `SetPSPinMode`, `SetPSMapMode`, `SetPSStabilize`, `SetPSIntsAndSpi`.
- Implemented them as backward-compatible wrappers that map the old parameters to the closest tunables of the WDSP 2.00 NURBS/spline calibration engine.
- The `PSForm.cs` switches are functional again; existing UI and user settings remain compatible.

### Mapping of legacy functions to the NURBS engine
| Function | Legacy meaning | New WDSP 2.00 behavior |
|---|---|---|
| `SetPSPtol` | Overdrive sample culling tolerance | `outlier_sigma = 1.5 + ptol * 1.875` for MAG/COS/SIN NURBS fits |
| `SetPSPinMode` | High-power endpoint pin mode | Toggles `pin_start` for COS/SIN; MAG end-pin kept enabled |
| `SetPSMapMode` | Amplitude grid mapping mode | Toggles `eq_enable` (density equalization) |
| `SetPSStabilize` | EMA smoothing of solutions | `stbl=1` -> `alpha=0.30`; `stbl=0` -> `alpha=1.0` (no smoothing) |
| `SetPSIntsAndSpi` | Integration count / samples per integration | Stores values and resets the collector for re-collection (WDSP 2.00 uses a fixed bucket collector) |

### Files changed
- `Project Files/Source/wdsp/calcc.h`: added `ptol`, `pin_mode`, `map_mode`, `stbl`, `ints`, `spi` fields and `extern` declarations.
- `Project Files/Source/wdsp/calcc.c`: field initialization, application of `ptol`/`pin_mode`/`map_mode` in `calc()`, implementation of the five `SetPS*` functions.
- `Project Files/Source/Console/PSForm.cs`: added `DllImport`s and re-enabled handlers for `chkPSRelaxPtol`, `chkPSPin`, `chkPSMap`, `chkPSStbl`, `comboPSTint`.

### Build status
- `wdsp.vcxproj` x64 Release: 0 errors.
- Full solution `Thetis_VS2026.sln` x64 Release: 0 errors, ~70 warnings.
- `dumpbin -exports`: all five functions are present in `wdsp.dll`.
- `Thetis.exe` starts and runs without crashing.

---

## 2026-07-07 — WDSP 2.00 integration completed (Q-factor EQ/CFCOMP ported)

### Summary
- Updated WDSP sources to version 2.00.
- Ported Thetis-specific patches: pixel_ref, CBL position, NR3/NR4.
- Adapted P/Invoke signatures for WDSP 2.00 (`GetPSDisp` 12 args, removed obsolete PS functions).
- Ported Q-factor parametric EQ and CFCOMP from the MW0LGE Thetis patch to the WDSP 2.00 NURBS architecture.
- Added `Yurij_eu2av` markers to modified C/C#/project files.

### Details

#### WDSP 2.00 source replacement
- Replaced `Project Files/Source/wdsp/*` with WDSP 2.00 sources.
- Added new files to `wdsp.vcxproj`: `extrapolate`, `nurbs*`, `phrot`, `reshb`, `snoop`, `wbfm`.
- Updated `Versions.cs` `_WDSP_VERSION` to `2000`.

#### Thetis patches ported
- `analyzer.c/h`: restored `SetPixelRef`, `GetPixels(..., double* pixel_ref)`, `PixelRefSection`.
- `cblock.c/h`: restored `position` parameter and `SetRXACBLPosition`.
- `rnnr.c/h`, `sbnr.c/h`: integrated into `RXA.c/h` and `comm.h`; extended `RXAbp1Check`.

#### P/Invoke adaptations
- `dsp.cs`: `GetPSDisp` 12 params; EQ/CFCOMP signatures with optional Q; removed obsolete PS imports.
- `PSForm.cs`, `AmpView.cs`: removed obsolete PS calls; updated `GetPSDisp`.
- `eqform.cs`, `frmCFCConfig.cs`, `setup.cs`: updated EQ/CFCOMP call sites.

#### Q-factor parametric EQ/CFCOMP port
- `eq.c/h`: added `Q` parameter to `eq_impulse` and `SetRXAEQProfile`/`SetTXAEQProfile`; Gaussian-blend Q path is active when `Q != NULL`, legacy linear/NURBS path when `Q == NULL`.
- `cfcomp.c/h`: added optional `Qg`/`Qe` to `SetTXACFCOMPprofile`; Gaussian-blend Q path in `calc_compG`/`calc_compE`.
- `fmsq.c`: updated `eq_impulse` calls for the new signature.
- C# call sites pass Q arrays when parametric mode is enabled, otherwise `NULL`.

### Known limitations
- Win32 (x86) build: `rnnoise.lib`/`specbleach.lib` are only available for x64. The x86 build requires adding x86 NR binaries or conditionally disabling NR3/NR4.

### Build status
- `wdsp.vcxproj` x64 Release: 0 errors.
- Full solution `Thetis_VS2026.sln` x64 Release: 0 errors, ~70 warnings.
- `dumpbin /exports`: all required imports present.
- `Thetis.exe` starts and runs without crash.

---

## 2026-07-04 (2) — 16-bit Float Pipeline + DeviceContext Migration + New Schemes + Bug Fixes

### Context
The Thetis waterfall was running in 8-bit (Format.B8G8R8A8_UNorm) via legacy Direct2D 1.0 (`ID2D1RenderTarget`). This limited quality: 256 levels per channel, visible banding on smooth gradients, a "plastic" look. Users compared it to SDR Console / HDSDR and asked for a modern waterfall.

### Achievements (in order)

#### 1. Direct2D 1.0 → 1.1 Migration (DeviceContext)
Legacy `RenderTarget` does not support 10/16-bit surface formats. Migrated to `ID2D1DeviceContext` (Direct2D 1.1+, Windows 8+):
- `Factory` → `Factory1`
- `new RenderTarget(factory, surface, rtp)` → `Device + DeviceContext + Bitmap1(Target)`
- D2D Device is created once, survives resize (otherwise `WRONG_RESOURCE_DOMAIN`)
- Fallback to legacy RenderTarget if DeviceContext cannot be created
- **Bonus:** DeviceContext is more resilient to device removal (GPU crash/driver update)

#### 2. 16-bit Float Pipeline (8/16-bit)
- Swap chain: `B8G8R8A8_UNorm` (8-bit, 256 levels) or `R16G16B16A16_Float` (16-bit half-float, 65536 levels) — selectable, live, no restart required
- 10-bit (`R10G10B10A2`) removed — Direct2D does not support it as a bitmap surface
- `WaterfallPixelWriter` — format-dependent encoder: float RGBA → packed bytes
- Float pipeline: all 8 schemes compute color in `float` (0..255), gamma/dither/quality in float, quantization only at output

#### 3. sRGB→Linear Compensation
On 16-bit float Direct2D operates in linear RGB; on 8-bit — in sRGB. Without compensation, colors looked different (brighter). Solution:
- `convertColour` → sRGB→linear for RGB + alpha at 16-bit
- `EncodeRow16` → sRGB→linear for waterfall pixels
- `SDXBitmapFromSysBitmap` → sRGB→linear + RGBA for background photo
- Stroke width scaling (`sw()`) for line thickness (linear coverage ~2× thicker)
- Alpha blending compensation (`pow(a, 1.6)`) for filter overlay

#### 4. Three New Color Schemes (WaterfallPalette)
Multi-stop gradient interpolator (256-step LUT) instead of hard-coded if/else bands:
- **Console 256** — SDR Console style: saturated rainbow gradient (black→navy→blue→cyan→green→yellow→orange→red→pink→white, 12 stops)
- **Thermal 256** — thermal camera / Inferno: warm gamma (black→indigo→purple→magenta→red→orange→yellow→white, 9 stops)
- **DeepBlue 256** — cool contrast: deep blue→cyan→mint→white, 8 stops
- Default is now **Console 256** (instead of enhanced)
- New schemes appear at the top of the combo list (RX1/RX2/TX)

#### 5. Quality Levels (replaced useless Palette Resolution)
Instead of an invisible LUT size change — visible effects in the float pipeline:
- **Classic** — no processing (default)
- **Vivid** — +30% saturation (pixel domain)
- **Sharp** — Vivid + 25% contrast (S-curve on signal percent, visible on enhanced)
- **Ultra** — Sharp + 30% contrast + auto-dither
- S-curve is applied to `overall_percent` BEFORE color selection → works on all schemes

#### 6. Bug Fix: Dim Waterfall After Custom (long-standing bug)
**Root cause:** Custom scheme, when the gradient was not loaded, performed a `break` from the switch, but the code after the switch (EncodeRow, CopyFromMemory) executed with an empty rowF (zeros = black). The black line was written to the bitmap every frame, overwriting the existing waterfall.
**Fix:** `skipRow` flag — the entire write pipeline is skipped if the scheme produced no color.

#### 7. Bug Fix: ADC Overload Flicker on TX→RX (long-standing bug)
**Root cause:** The ADC overload flag in native code (`network.c`) is latched via `||` and is not reset on the TX→RX transition. The first RX poll read the stale latched overload → brief "ADC0 Overload" warning.
**Fix:** `UIMOXChangedFalse()` now resets the native latch via `NetworkIO.getAndResetADC_Overload()` + C# counters `_adc_overload_level[]`. Safe: does not touch FPGA/native code, only clears stale state.

### New Files (2)
| File | Purpose |
|---|---|
| **`Console/WaterfallPixelWriter.cs`** | Format-dependent encoder: `EncodeRow` (8/16-bit), `FillClearBuffer`, `SrgbToLinear`, `FloatToHalfBitsPublic` |
| **`Console/WaterfallPalette.cs`** | Multi-stop gradient interpolator: `Build(Stop[])`, `Sample(percent)`, ready-made palettes ConsoleStops/ThermalStops/DeepBlueStops |

### Modified Files
| File | Changes |
|---|---|
| `Console/display.cs` | DeviceContext migration; createD2DRenderTarget; RebuildForColorDepth; sRGB→linear; sw() stroke scaling; 3 new cases in switch; DrawPaletteScheme helper; skipRow bug fix; alpha compensation |
| `Console/WaterfallEnhancer.cs` | ColorDepth enum; QualityLevel enum + SetQuality; ApplyQualityPercent (S-curve); ApplySaturationContrast; ApplyGammaFloat/ApplyDitherFloat (float versions) |
| `Console/enums.cs` | +Console, +Thermal, +DeepBlue in ColorScheme |
| `Console/setup.cs` | comboColorDepth (8/16-bit, live rebuild); Quality combo (Classic/Vivid/Sharp/Ultra); handlers; Sync |
| `Console/setup.designer.cs` | +3 options in RX1/RX2/TX combo (Console 256, Thermal 256, DeepBlue 256) |
| `Console/console.cs` | ADC overload reset in UIMOXChangedFalse (TX→RX bug fix) |
| `Console/Thetis.csproj` | +WaterfallPixelWriter.cs, +WaterfallPalette.cs |

### Safety
- **Default = Console 256 + 8-bit + Classic Quality** — zero regressions
- Does not touch FPGA firmware or native code (except reading ADC overload)
- Existing schemes (enhanced, SPECTRAN, LinRad...) remain unchanged
- Fallback to legacy RenderTarget if GPU does not support DeviceContext
- Live switching 8↔16-bit without restart, with fallback to 8-bit on error
- All settings are saved to database.xml

---

## 2026-07-04 — Waterfall Quality Enhancement (Custom Scheme LUT + Dither + Gamma)

### Context
After the DPI-awareness fix, the waterfall became sharper on HiDPI/2K, but users noted the effect was "barely noticeable" — colors looked flat/dull, banding was visible on smooth gradients. Analysis showed: the rendering architecture (Direct2D) was already high-quality, but the **Custom scheme color palette** was limited to only **101 LUT steps** without dithering and with linear dB→color mapping.

### Solution — New `WaterfallEnhancer` Subsystem
A separate static class (`WaterfallEnhancer.cs`) with three quality levers. All settings default to current behavior (zero regressions), stored in database.xml, carried with the profile.

#### 1. Palette Resolution (101 → 256/512/1024)
Expanding the LUT from fixed 101 to a selectable size. The palette is now sampled from the gradient editor (`ucLGPicker`) at the required density.
- **Effect:** smooth gradients, disappearance of "bands" / streaks.

#### 2. Color Dithering (Ordered Bayer 8×8)
Light ordered dithering (±2 LSB) is applied to RGB before writing to the pixel.
- **Effect:** bands are completely masked, even with a 101-step palette.

#### 3. Gamma Curve
A power curve is applied to `overall_percent` before LUT lookup.
- gamma < 1.0 → "stretches" weak signals (more detail in noise)
- gamma > 1.0 → enhances contrast of strong signals
- gamma = 1.0 → linear (default)

### Files (4)
| File | Change |
|---|---|
| **`Console/WaterfallEnhancer.cs`** (NEW) | Static class: `LUT_SIZE`, `DitherEnabled`, `Gamma`, Bayer 8×8 matrix, methods `ApplyGamma()`, `ApplyDitherChannel()` |
| `Console/display.cs` | Arrays `Color[101]` → `Color[LUT_SIZE]` (dynamic); guards `!= 101` → dynamic; lookup `cols[100]` → `cols[lut-1]`, `* 100f` → `* (lut-1)`; gamma + dither in Custom scheme |
| `Console/setup.cs` | Control fields; `InitWaterfallQualityControls()` (programmatic creation in grpDisplayDriverEngine); handlers; `SyncWaterfallEnhancerFromControls()` after getOptions; `WaterfallRXGradient()`/`WaterfallTXGradient()` → dynamic size |
| `Console/setup.designer.cs` | NOT touched — controls are created programmatically |

### UI
In the **"DirectX Display Settings"** group (Display tab), below chkDpiAwareness:
- **Palette:** ComboBox ("101 Classic" / "256 High" / "512 Ultra" / "1024 Max")
- **Dither:** CheckBox
- **Gamma:** TrackBar (0.50–2.00) + value label

All controls have unique names → auto-save via `SaveOptions/getOptions`.

### Safety
- **Default = current behavior** (LUT 101, dither OFF, gamma 1.0)
- Does not touch the gradient editor (`ucLGPicker`) — it works as before
- Does not touch enhanced/SPECTRAN — only Custom scheme (where LUT is used)
- Works on top of existing code — extension point for future improvements

### User Procedure
1. Setup → Display → "DirectX Display Settings"
2. **Palette:** select "256 High" (or higher for a powerful GPU)
3. **Dither:** enable (removes banding)
4. **Gamma:** adjust to taste (0.8 stretches weak signals)
5. OK → settings are saved to database.xml

### Backups
- `display.cs.wfq_backup_20260703`
- `setup.cs.wfq_backup_20260703`

### Future
- Removing dead code in old schemes (enhanced/SPECTRAN/original)
- Migrating all schemes to a unified LUT architecture via WaterfallEnhancer
- Extended gradient editor (more stops, curves)
- Auto-contrast / noise floor tracking in WaterfallEnhancer

---

## 2026-07-03 (2) — Voltage Calibration (PA Volts + Supply 13.8V)

### Problem
Thetis had calibration for **current** (AmpVoff/AmpSens in the "Current (A) calculation" group), but **voltage** was calculated via `convertToVolts` (PA Volts, AIN3) and `computeHermesDCVoltage` (Supply 13.8V, AIN6) with **hard-coded** dividers and no adjustment whatsoever. After the crosstalk fix on the FPGA side (dummy-conversion changed ADC polling timing), the voltage readings shifted slightly (13.8 → 13.9V).

### Solution
Added **per-device multipliers** for both voltage paths:
- `PAVoltCal` — for PA Voltage (AIN3, `convertToVolts`)
- `SupplyVoltCal` — for Supply 13.8V (AIN6, `computeHermesDCVoltage`)

`k = 1.000` → no correction (default, zero regressions). The multiplier is applied to the final voltage value. Example: shows 13.9V when actual is 13.8 → `k = 13.8/13.9 = 0.993`.

### Files (2)
| File | Change |
|---|---|
| `Console/console.cs` | Properties `PAVoltCal`/`SupplyVoltCal`; multipliers in `convertToVolts` (~24959) and `computeHermesDCVoltage` (~24811) |
| `Console/setup.cs` | Fields `udPAVoltCal`/`udSupplyVoltCal` + labels; programmatic creation in `initVoltsAmpsCalibration`; handlers `*_ValueChanged` |

### UI
In the **"Current (A) calculation"** group (Calibration tab), at the bottom (below existing current calibrations and the Log Volts/Amps checkbox), added:
- **"PA V cal:"** (0.100–5.000, step 0.001, default 1.000) — for PA Voltage (AIN3)
- **"Supply V cal:"** (0.100–5.000, step 0.001, default 1.000) — for Supply 13.8V (AIN6)
- **"V Default" button** — resets both multipliers to 1.000 (no correction)

The group height was increased (227→300) to accommodate the new controls without overlapping existing ones. Controls are created programmatically in `initVoltsAmpsCalibration` and call `BringToFront()` for correct Z-order.

**Persistence:** Controls `udPAVoltCal`/`udSupplyVoltCal` with unique names are automatically saved/loaded via the existing `SaveOptions()`/`getOptions()` mechanism (keyed by control name).

### Calibration Procedure
1. Setup → Calibration tab → "Current (A) calculation" group
2. Check actual voltage with an external multimeter (power supply or PA)
3. If Thetis shows X V when actual is Y → `cal = Y / X`  
   (example: shows 13.9V when actual is 13.8 → Supply V cal = 13.8/13.9 = 0.993)
4. Same for PA Volts
5. OK → settings are saved to database.xml
6. **"V Default"** button — quick reset to 1.000

### Backups
- `console.cs.volts_backup_20260703`
- `setup.cs.volts_backup_20260703`

---

## 2026-07-03 — Per-Monitor DPI Awareness Toggle (waterfall quality on 2K+)

### Problem
On HiDPI/2K monitors, the waterfall and panadapter looked blurry / smeared, with "low bit depth" gradients — noticeably worse than on HD. Root cause: the Thetis process was **not declared DPI-aware** (the `<dpiAware>` block in `app.manifest` was commented out, `SetProcessDpiAwareness(2)` in Main was commented out). The D2D swap-chain and waterfall bitmap received **logical** resolution (~1280px), and the DWM compositor stretched it to physical ~2560px → blur.

### Solution — Per-Monitor V2 Toggle (registry-backed)
Following the pattern of the existing `chkShowStartupLog` (registry, read early in Main): the flag is stored in `HKCU\Software\OpenHPSDR\Thetis-x64\DpiAwareness` (DWORD). Applied at the very beginning of `Main()`, before any window is created.

When toggle is ON:
- `SetProcessDpiAwarenessContext(PER_MONITOR_V2)` (Win10 1703+)
- fallback `SetProcessDpiAwareness(PER_MONITOR_AWARE)` (Win8.1)
- try/catch for older OSes

**Effect is automatic:** swap-chain (`displayTarget.Width/Height`), waterfall bitmap and FFT-pixel count (`pixels = displayTargetWidth`) start receiving **physical** resolution — without changes to `display.cs`. AntiAlias for the panadapter is already enabled by default (`chkAntiAlias.Checked = true`).

### Files (4)
| File | Change |
|---|---|
| `clsProgressLog.cs` (LogTool) | `GetRegistryDpiAwareness`/`SetRegistryDpiAwareness` + read/write helpers (following the ShowLog pattern) |
| `console.cs` | P/Invoke `SetProcessDpiAwarenessContext` + `SetProcessDpiAwareness`; applied at the beginning of Main (after args tidy, before SingleInstance); removed old commented-out call |
| `setup.cs` | Field `chkDpiAwareness`; `CreateDpiAwarenessCheckBox()` (programmatic creation, next to chkShowStartupLog); `chkDpiAwareness_CheckedChanged` (MessageBox "restart required"); `updateDpiAwarenessCheckBox`; exclusion from DB in `getOptions` |
| `setup.designer.cs` | NOT touched — checkbox is created programmatically |

### UI
Checkbox **"DPI Awareness (HiDPI/2K)"** on the Display tab → "DirectX Display Settings" group (next to chkAntiAlias — both relate to rendering quality). Created programmatically (`CreateDpiAwarenessCheckBox`), positioned below chkAntiAlias. When toggled → MessageBox "Restart Required" → user restarts.

### Safety
- **Default OFF** → on first run, behavior is identical to the original
- Registry (not DB) → read at the beginning of Main, before forms (DB loads too late)
- WinForms UI is scaled by the OS → if it breaks on a particular system, the user unchecks the box → restart → rollback
- Restart flow is already built into Main (`Application.Restart()`)

### User Procedure
1. Setup → Display tab → "DirectX Display Settings" group → check "DPI Awareness (HiDPI/2K)"
2. Check → MessageBox "Restart Required" → OK → restart
3. Waterfall/panadapter become sharp on 2K
4. If UI breaks → uncheck → restart (rollback)

### Backups
- `clsProgressLog.cs.dpi_backup_20260703`
- `console.cs.dpi_backup_20260703`
- `setup.cs.dpi_backup_20260703`
- `setup.designer.cs.dpi_backup_20260703`

---

## 2026-07-02 — Per-band Power Detector Calibration

### Problem
Standard power calibration in Thetis is global (drive calibration); it does not account for the frequency dependence of the directional coupler (tandem-match coupler). On LF bands (160/80/40m), power readings were 4–12% low (160m: ~91W with actual 100W into the load), because the detector physically outputs a different voltage at lower frequencies. SWR remained correct, since the distortion of FWD and REV was identical.

Thetis had no per-band FWD/REV detector calibration. The existing method `PABandOffset(Band)` (console.cs:6607) was commented out / returned 0 and was not used in the live path (Path A — `computeAlexFwdPower`/`computeRefPower` → `alex_fwd`/`alex_rev` → meters).

### Solution
Added a **per-band multiplier `k`** to `bridge_volt` in the formula `watts = V²/bridge_volt`:
- `watts_corr = V² · k / bridge_volt`
- `k = 1.00` → no correction (zero regressions, behavior identical to current)
- `k > 1` → readings higher (if it was reading low)
- `k < 1` → readings lower

The multiplier is **shared for FWD and REV** (applied equally), so SWR (= √(Rev/Fwd)) does not change — this is physically correct when both detectors have the same frequency dependence.

### Files (3)
| File | Change |
|---|---|
| `Console/console.cs` | Method `PABandCalMult(Band)`; formula fix in `computeAlexFwdPower` (~25120) and `computeRefPower` (~25040) — Path A, live meters |
| `Console/setup.cs` | `GetPABandCalMult(Band)` + `GetPABandCalControl(Band)`; `InitDetCalTab()` — programmatic creation of the UI tab (after `InitializeComponent()`) |
| `Console/setup.designer.cs` | Declarations of 11 control fields + 1 field `tpDetCal` (no instantiation — everything is in setup.cs) |

### UI
New **"Det. Cal."** tab in Setup → PA Settings (third after "PA Gain" and "Watt Meter"). Contains 11 `NumericUpDownTS` fields (160m..6m), each:
- `Minimum = 0.50`, `Maximum = 2.00`, `Increment = 0.01`, `DecimalPlaces = 2`
- `Value = 1.00` (default)
- Explanatory text: "k = 1.00 = no correction. k > 1 increases reading..."
- **"Reset to Defaults"** button (on the right) — restores model-dependent factory values, overwriting saved ones.

**Persistence:** Controls are automatically saved/loaded via the existing `SaveOptions()`/`getOptions()` mechanism (setup.cs:1519/1706) — keyed by control name (`udDetCalB160M`, etc.). No additional serialization code is required.

### Defaults for Anvelina PRO3 / ANAN7000D
Factory values are defined for `HPSDRModel.ANVELINAPRO3` and `HPSDRModel.ANAN7000D` (in Thetis these are the same family), calibrated against an external wattmeter into a dummy load (2026-07-02):

| Band | k | Note |
|---|---|---|
| 160m | 0.91 | compensates ~9% over-read |
| 80m  | 0.92 | compensates ~8% over-read |
| 40m  | 0.96 | compensates ~4% over-read |
| 60m, 30m, 20m, 17m, 15m, 12m, 10m, 6m | 1.00 | no correction |

**Default application logic (two levels):**
1. **On startup** — method `ApplyDetCalDefaults()` is called in the Setup constructor AFTER `getModelFromDB()` (when the model is known) and BEFORE `getOptions()`. If the user has saved values in database.xml — `getOptions()` correctly overwrites defaults with their settings (user priority).
2. **On "Reset to Defaults" button** — `btnDetCalReset_Click` resets all fields to 1.00, then applies `ApplyDetCalDefaults()`. The user presses OK → new values are saved to database.xml.

This solves the "locked-in values" problem: if 1.00 was saved during the first runs, the Reset button allows restoring factory calibration in one click.

### User Calibration Procedure
**Quick start (Anvelina PRO3 / ANAN7000D):**
1. Setup → PA Settings → **"Det. Cal."** tab
2. Press **"Reset to Defaults"** → factory values are applied (0.91/0.92/0.96)
3. OK → settings are saved to database.xml

**Manual calibration (any model):**
1. Setup → PA Settings → **"Det. Cal."** tab
2. Reference band (e.g., 20m): leave 1.00
3. Apply calibrated power (100W) on the band, compare with an external wattmeter
4. If Thetis shows X W when actual is 100 → set `k = 100 / X`  
   (example: shows 110W → k = 100/110 = 0.91)
5. OK → settings are saved to database.xml

### Why Path A Was Chosen (Not Path B)
The Thetis code has two power paths:
- **Path A (live):** `computeAlexFwdPower`/`computeRefPower` → `alex_fwd`/`alex_rev` → `PollPAPWR` → meters/CAT. **In use.**
- **Path B (dead):** `PABandOffset`/`ScaledVoltage`/`ADCtodBm` → always returns 0, not called from live code. **Not touched.**

The fix was applied in Path A, where the actual readings are formed.

### Safety
- **k = 1.0 by default** → behavior identical to original Thetis
- Does not touch Path B (dead code)
- Does not touch `PAProfile` (separate per-band gain subsystem)
- Does not touch existing PA Gain / Watt Meter tabs
- Auto-save via the standard mechanism (zero persistence code)

### Backups
- `Console/console.cs.detcal_backup_20260702`
- `Console/setup.cs.detcal_backup_20260702`
- `Console/setup.designer.cs.detcal_backup_20260702`

---

## Related Work (FPGA Side)

These Thetis changes complement the work on the FPGA side (Anvelina PRO III / Orion MK2, firmware version 2.2.14), where display jitter was eliminated (crosstalk/ghosting in the ADC78H90 multiplexer via dummy-conversion). Full FPGA-side documentation is in `CHANGELOG_Yurij_eu2av.md` of the Verilog project repository.
