# Thetis v2.10.3.15-extended (1.0) — Yurij-eu2av Enhanced Fork

Enhanced fork of Thetis SDR for openHPSDR transceivers. Built with VS2026, x64 Release.

---

## What's New in (1.0)

### 1. CW APF (Audio Peaking Filter) — Correct Defaults
- Default gain raised to **6 dB** (was 0 dB) and bandwidth to **100 Hz** (was 150 Hz) per WDSP Guide recommendations.
- Gain label now shows units: "Gain: X dB".
- Telegraphists reported APF modes (BI/DP/MA/GA) sounding less pronounced — root cause was Thetis defaults, not WDSP algorithms.

### 2. FFT Wisdom — Fix Repeated Warning
- Fixed bug where "fft wisdom file is missing" appeared on **every** launch even after successful wisdom generation.
- C# code was checking for obsolete `wdspWisdom00`; now correctly checks `wdspWisdom01`.
- Legacy `wdspWisdom00` auto-renamed or removed on first run.

### 3. Auto Database Upgrade on Schema Mismatch
- Automatic detection of outdated/corrupted database after adding new features/columns.
- Checks: TXProfile columns, DBNull in required fields, State VersionNumber, default TXProfileDef.
- If incompatible — warning dialog with **Yes/No** consent; automatic backup created before upgrade.
- **Ctrl on launch no longer required** — dialog appears automatically. Ctrl and `updatedb.txt` remain as manual fallback.

### 4. APF Button — Proper Skinning + Independent SkinsAPF Folder
- APF type button (`btnAPF_type`) converted to `CheckBoxTS` with `Appearance = Button` for correct skin engine handling.
- Labels DP/MA/GA/BI now readable: white bold text, flat style, dark background.
- New independent **`SkinsAPF/`** folder next to `Skins/` — APF images load from there if missing in current skin.
- Folder auto-created on first launch with default PNG stubs; user can replace with custom images.

### 5. WDSP 2.00 — Phase Rotator (Auto/Reset/Asymmetry) + PSA Over-Drive Indicator
- Complete Phase Rotator implementation per WDSP 2.00 Guide:
  - **Auto FC** checkbox — enable/disable auto-optimization of angular frequency;
  - **Reset** button — reset optimizer to 338 Hz and restart search;
  - **IN/OUT** asymmetry indicators + current **FC** frequency;
  - Optimizer status: **Off / Search / Done**.
- PureSignal visual warning: `lblPSInfo6` highlights **red** on severe over-drive (`info[6] == 2`).

### 6. Branding & Author Info
- Main window title: added **"extended version-eu2av"**.
- About dialog: added **EU2AV, Yurij** to contributors list — PureSignal enhancements, feedback calibration, Anvelina PRO3 firmware.

### 7. PureSignal — Feedback Level Calibration / Auto-ATT + Outlier Filter
- Configurable **Feedback Level target** for PureSignal.
- Orion MK2 platforms (ANAN-7000D, ANAN-8000D, ANVELINAPRO3): default target **22** (was hardcoded 152).
- Optimal working point: ATT ≈ 10 dB, feedback doesn't overload ADC2208/codec, IMD stays clean.
- **Outlier filter** for cubic-spline engine: Orion MK2 ON by default (sigma = 5.0), others OFF.

### 8. Build Cleanup — Zero Warnings
- Eliminated causes (not suppression) of MSB8012, CSxxxx, MSB3884 warnings.
- C++ projects: aligned `OutDir`/`TargetName`/`TargetExt` with `Link.OutputFile`.
- C#: removed unused variables/fields; proper exception logging.
- x64 Release: **0 errors, 0 warnings**.

### 9. WDSP 1.x PureSignal Engine — Proven Quality
- After extensive NURBS/spline testing on real ANAN-7000 / Anvelina PRO III hardware, reverted to proven cubic-spline PureSignal engine from WDSP 1.x.
- New NURBS engine produced IMD "skirt" unremovable by manual attenuation, `rx_scale`, or PS Peak adjustments.
- WDSP 1.x core (`calcc` + `iqc`) provides same correction quality as previous working build.
- All WDSP 2.0 UI functions mapped to WDSP 1.x engine via `__declspec(dllexport)` stubs; `EMA α` controls real smoothing.

### 10. WDSP 2.00 — Advanced PureSignal Controls Restored
- Restored `SetPSPtol`, `SetPSPinMode`, `SetPSMapMode`, `SetPSStabilize`, `SetPSIntsAndSpi` exports.
- Mapped to nearest NURBS/spline settings for WDSP 2.00 engine.
- All five controls functional in `PSForm.cs`.

### 11. WDSP 2.00 Integration — Q-Factor EQ/CFCOMP
- Full WDSP 2.00 source replacement with Thetis-specific patches (pixel_ref, CBL position, NR3/NR4).
- P/Invoke signatures adapted (`GetPSDisp` 12 args, EQ/CFCOMP with optional Q).
- Q-factor parametric EQ and CFCOMP ported to NURBS architecture.

### 12. 16-Bit Float Waterfall Pipeline + DeviceContext Migration
- **Direct2D 1.0 → 1.1 (DeviceContext)** migration for 10/16-bit surface support.
- **16-bit float pipeline** (`R16G16B16A16_Float`) — 65536 levels vs 256; selectable, live, no restart.
- **sRGB→linear compensation** for consistent colors across 8/16-bit modes.
- **Three new color schemes**: Console 256, Thermal 256, DeepBlue 256 (multi-stop gradient interpolator).
- **Quality levels**: Classic / Vivid (+30% saturation) / Sharp (+25% contrast) / Ultra (+30% contrast + auto-dither).
- **Bug fix**: Custom scheme "black waterfall" — fixed skipRow logic.
- **Bug fix**: ADC overload flash on TX→RX — native latch now properly reset.

### 13. Waterfall Enhancer — Palette Resolution / Dithering / Gamma
- **Palette Resolution**: 101 → 256/512/1024 steps (smoother gradients, no banding).
- **Ordered Bayer 8×8 dithering**: masks bands even at 101 steps.
- **Gamma curve**: adjusts `overall_percent` before LUT lookup (<1.0 pulls weak signals, >1.0 boosts contrast).
- All defaults = current behavior (zero regressions).

### 14. Voltage Calibration — PA Volts + Supply 13.8V
- Per-device multipliers for **PA Voltage** (AIN3) and **Supply 13.8V** (AIN6).
- Default k = 1.000 (no correction). Calibrate with external multimeter: `k = real / shown`.
- UI: Setup → Calibration → "PA V cal" / "Supply V cal" + "V Default" reset button.

### 15. Per-Monitor DPI Awareness (HiDPI/2K)
- Toggle **"DPI Awareness (HiDPI/2K)"** in Setup → Display → DirectX Display Settings.
- Enables `PER_MONITOR_V2` awareness; swap-chain and waterfall get **physical** resolution.
- **Default OFF** — identical to original Thetis. Requires restart.

### 16. Per-Band Power Detector Calibration
- Per-band multiplier `k` for FWD/REV detector (160m–6m).
- Corrects frequency-dependent tandem-match coupler response (e.g., 160m reads ~9% high).
- Common multiplier for FWD/REV → SWR unchanged (physically correct).
- Factory defaults for Anvelina PRO3 / ANAN7000D: 160m=0.91, 80m=0.92, 40m=0.96, others=1.00.
- UI: Setup → PA Settings → **"Det. Cal."** tab + "Reset to Defaults" button.

---

## System Requirements
- Windows 10/11 x64
- .NET Framework 4.8
- Visual C++ Redistributable 2015-2022 (x64) — included in installer

## Installation
1. Download `Thetis-v2.10.3.15.x64 extended version(1.0).msi` from Releases
2. Run installer (admin rights required)
3. Thetis auto-detects database version and offers upgrade on first run

## Build
Requires **Visual Studio 2022** (or VS2026 solution) with .NET Framework / .NET Desktop workload.

```bash
# Open solution
Project Files\Source\Thetis_VS2026.sln

# Build platform: x64
# Target framework: .NET Framework 4.8
```

**Offline build:** NuGet packages will be restored from internet on first build.

## Credits
- Base: Thetis 2.10.3.15 by MW0LGE / OpenHPSDR team
- Extended fork: **Yurij-eu2av** (EU2AV)
- FPGA companion: Anvelina PRO III / Orion MK2 firmware v2.2.14

## Support
- GitHub Issues: https://github.com/eu2av/OpenHPSDR-Thetis-Enhanced/issues
- Full changelog: `CHANGELOG_Yurij-eu2av_EN.md` in repository
