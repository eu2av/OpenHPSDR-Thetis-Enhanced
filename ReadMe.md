# Thetis v2.10.3.15-extended (1.0) — Yurij-eu2av Enhanced Fork

Enhanced fork of Thetis SDR for openHPSDR transceivers. Built with VS2026, x64 Release.

## What's New in (1.0)

### 1. CW APF (Audio Peaking Filter) — Correct Defaults
- Default gain raised to **6 dB** (was 0 dB) and bandwidth to **100 Hz** (was 150 Hz).
- Gain label now shows units: "Gain: X dB".

### 2. FFT Wisdom — Fix Repeated Warning
- Fixed bug where "fft wisdom file is missing" appeared on every launch.
- Now correctly checks `wdspWisdom01`.

### 3. Auto Database Upgrade on Schema Mismatch
- Automatic detection of outdated database with Yes/No consent dialog.
- Backup created automatically before upgrade.
- Ctrl on launch no longer required.

### 4. APF Button — Proper Skinning + Independent SkinsAPF Folder
- APF button converted to `CheckBoxTS` for correct skin engine handling.
- New independent `SkinsAPF/` folder with default PNG stubs.

### 5. WDSP 2.00 — Phase Rotator + PSA Over-Drive Indicator
- Auto FC, Reset, IN/OUT asymmetry indicators, optimizer status.
- PureSignal visual warning on severe over-drive.

### 6. Branding & Author Info
- Main window title: added "extended version-eu2av".
- About dialog: added EU2AV, Yurij to contributors.

### 7. PureSignal — Feedback Level Calibration / Auto-ATT + Outlier Filter
- Configurable Feedback Level target for Orion MK2 platforms (default 22).
- Outlier filter for cubic-spline engine.

### 8. Build Cleanup — Zero Warnings
- Eliminated causes of MSB8012, CSxxxx, MSB3884 warnings.
- x64 Release: **0 errors, 0 warnings**.

### 9. WDSP 1.x PureSignal Engine — Proven Quality
- Reverted to proven cubic-spline PureSignal engine from WDSP 1.x.

### 10. WDSP 2.00 — Advanced PureSignal Controls Restored
- Restored `SetPSPtol`, `SetPSPinMode`, `SetPSMapMode`, `SetPSStabilize`, `SetPSIntsAndSpi`.

### 11. WDSP 2.00 Integration — Q-Factor EQ/CFCOMP
- Full WDSP 2.00 source replacement with Thetis-specific patches.

### 12. 16-Bit Float Waterfall Pipeline + DeviceContext Migration
- Direct2D 1.0 → 1.1 migration for 10/16-bit surface support.
- 16-bit float pipeline (65536 levels vs 256).
- Three new color schemes: Console 256, Thermal 256, DeepBlue 256.

### 13. Waterfall Enhancer — Palette Resolution / Dithering / Gamma
- Palette Resolution: 101 → 256/512/1024 steps.
- Ordered Bayer 8×8 dithering.
- Gamma curve adjustment.

### 14. Voltage Calibration — PA Volts + Supply 13.8V
- Per-device multipliers for PA Voltage and Supply 13.8V.

### 15. Per-Monitor DPI Awareness (HiDPI/2K)
- Toggle "DPI Awareness (HiDPI/2K)" in Setup → Display.

### 16. Per-Band Power Detector Calibration
- Per-band multiplier for FWD/REV detector (160m–6m).
- Factory defaults for Anvelina PRO3 / ANAN7000D.

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
