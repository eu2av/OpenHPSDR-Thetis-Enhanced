// Yurij-eu2av - 2026-07-04: Waterfall Quality Enhancer
// A standalone, non-invasive subsystem that improves waterfall visual quality
// by post-processing the color mapping. All settings are optional (default =
// classic behaviour = zero regressions) and stored in database.xml via the
// standard SaveOptions/getOptions mechanism (control-name keyed).
//
// Levers:
//   1. Color Depth         — 8 / 10 / 16-bit output. Pipeline works in float
//                            internally; WaterfallPixelWriter quantises to the
//                            target format at the very end. 8 = classic, 10 =
//                            modern HDR (1024 levels), 16 = float (65536).
//   2. Palette Resolution  — expands the Custom-scheme LUT from 101 to
//                            256/512/1024 entries, eliminating colour banding
//                            on smooth gradients.
//   3. Colour Dithering    — ordered Bayer 8x8 dither applied to the float
//                            channels before quantisation. Scales its delta to
//                            the active depth so it is visible at 8/10-bit and
//                            self-disables at 16-bit.
//   4. Gamma Curve         — applies a power curve to the dB->percent mapping
//                            (Custom) and to the final RGB (all schemes),
//                            letting the user lift weak signals (gamma < 1)
//                            or boost strong-signal contrast (gamma > 1).
//
// Designed as a future extension point: additional post-processing (e.g. noise
// floor tracking, auto-contrast) can be added here without touching display.cs.

using System;
using System.Drawing;

namespace Thetis
{
    public static class WaterfallEnhancer
    {
        // ---- Color depth -------------------------------------------------------
        // Selects the output surface format. 8-bit = classic behaviour.
        public enum ColorDepth { Bit8, Bit10, Bit16 }
        public static ColorDepth Depth { get; private set; } = ColorDepth.Bit8;

        // ---- Quality level (replaces "Palette Resolution") --------------------
        // Yurij-eu2av - 2026-07-04: the old Palette control changed only the LUT
        // size, which was invisible in practice. Quality now drives VISIBLE float
        // post-processing: saturation, contrast and dither. Each level stacks.
        //   Classic = no processing (original look)
        //   Vivid   = +30% saturation
        //   Sharp   = Vivid + contrast boost
        //   Ultra   = Sharp + auto-dither (smoother gradients)
        // Works for ALL colour schemes (enhanced, Custom, SPECTRAN, ...) because
        // it runs in the unified float pipeline after colour mapping.
        public enum QualityLevel { Classic, Vivid, Sharp, Ultra }
        public static QualityLevel Quality { get; private set; } = QualityLevel.Classic;
        public static float SaturationBoost { get; private set; } = 0f;   // 0..1, fraction added
        public static float ContrastBoost { get; private set; } = 0f;     // 0..1, amount around 0.5

        // ---- Palette resolution (kept for Custom LUT, default 101) -------------
        public static int LUT_SIZE { get; private set; } = 101;

        // ---- Dithering ----------------------------------------------------------
        public static bool DitherEnabled { get; private set; } = false;

        // ---- Gamma --------------------------------------------------------------
        // 1.0 = linear (no change). Range 0.5..2.0.
        public static float Gamma { get; private set; } = 1.0f;
        private static float _invGamma = 1.0f; // cached reciprocal

        // ---- Bayer 8x8 ordered dither matrix (values 0..63) --------------------
        // Standard Bayer dispersed-dot pattern. Delta scales with depth so it
        // is perceptible at 8/10-bit and vanishes at 16-bit.
        private static readonly int[,] BAYER_8X8 = {
            {  0, 32,  8, 40,  2, 34, 10, 42 },
            { 48, 16, 56, 24, 50, 18, 58, 26 },
            { 12, 44,  4, 36, 14, 46,  6, 38 },
            { 60, 28, 52, 20, 62, 30, 54, 22 },
            {  3, 35, 11, 43,  1, 33,  9, 41 },
            { 51, 19, 59, 27, 49, 17, 57, 25 },
            { 15, 47,  7, 39, 13, 45,  5, 37 },
            { 63, 31, 55, 23, 61, 29, 53, 21 }
        };

        // ---- Public setters (called from Setup form handlers) ------------------
        public static void SetColorDepth(ColorDepth depth)
        {
            Depth = depth;
        }

        public static void SetPaletteResolution(int size)
        {
            // constrain to allowed values
            if (size == 101 || size == 256 || size == 512 || size == 1024)
                LUT_SIZE = size;
        }

        // Yurij-eu2av - 2026-07-04: set the quality level and derive the
        // saturation/contrast/dither boost that the float pipeline applies.
        public static void SetQuality(QualityLevel level)
        {
            Quality = level;
            switch (level)
            {
                default:
                case QualityLevel.Classic:
                    SaturationBoost = 0f;
                    ContrastBoost = 0f;
                    break;
                case QualityLevel.Vivid:
                    SaturationBoost = 0.30f;
                    ContrastBoost = 0f;
                    break;
                case QualityLevel.Sharp:
                    SaturationBoost = 0.30f;
                    ContrastBoost = 0.25f;
                    break;
                case QualityLevel.Ultra:
                    SaturationBoost = 0.40f;
                    ContrastBoost = 0.30f;
                    break;
            }
        }

        public static void SetDither(bool enabled)
        {
            DitherEnabled = enabled;
        }

        public static void SetGamma(float gamma)
        {
            // clamp to a sane range
            if (gamma < 0.5f) gamma = 0.5f;
            if (gamma > 2.0f) gamma = 2.0f;
            Gamma = gamma;
            _invGamma = 1.0f / gamma;
        }

        // ---- Quantisation levels per channel (drives dither magnitude) --------
        public static int Levels
        {
            get
            {
                switch (Depth)
                {
                    case ColorDepth.Bit8:  return 255;
                    case ColorDepth.Bit10: return 1023;
                    case ColorDepth.Bit16: return 65535;
                    default:               return 255;
                }
            }
        }

        // ---- Post-processing helpers (called from display.cs) -------------------

        /// <summary>
        /// Apply the gamma curve to a linear 0..1 percentage.
        /// gamma = 1.0 returns the value unchanged.
        /// </summary>
        public static float ApplyGamma(float percent)
        {
            if (percent <= 0f) return 0f;
            if (percent >= 1f) return 1f;
            if (Gamma == 1.0f) return percent;
            return (float)Math.Pow(percent, _invGamma);
        }

        /// <summary>
        /// Yurij-eu2av - 2026-07-04: apply the Quality contrast curve to the signal
        /// percentage (0..1) BEFORE colour mapping. This shapes where weak vs strong
        /// signals land in the palette, so it is visible on EVERY scheme (including
        /// enhanced, whose colours are already at full saturation).
        ///
        /// The curve pushes values away from 0.5: weak signals get darker, strong
        /// signals get brighter. Classic = identity.
        /// </summary>
        public static float ApplyQualityPercent(float percent)
        {
            if (ContrastBoost <= 0f) return percent;
            if (percent <= 0f) return 0f;
            if (percent >= 1f) return 1f;
            // S-curve: (0.5 - cos(pi*p)/2) blended with linear by ContrastBoost.
            // Smoother than a hard (p-0.5)*c+0.5 and avoids overshoot.
            float scurve = 0.5f - (float)Math.Cos(Math.PI * percent) * 0.5f;
            return percent + (scurve - percent) * ContrastBoost;
        }

        /// <summary>
        /// Yurij-eu2av - 2026-07-04: apply saturation + contrast boost to a float
        /// RGB pixel (channels 0..255). Called from the unified post-processing
        /// pass for ALL colour schemes. No-op at Quality=Classic.
        ///
        /// Saturation pushes colours away from grey (toward pure hue). Contrast
        /// pushes luminance away from mid-grey (toward black/white). Both run in
        /// float before quantisation so they are depth-agnostic.
        /// </summary>
        public static void ApplySaturationContrast(float[] rowF, int idx)
        {
            if (SaturationBoost <= 0f && ContrastBoost <= 0f) return;

            float r = rowF[idx + 0];
            float g = rowF[idx + 1];
            float b = rowF[idx + 2];

            // Saturation: move RGB toward/away from the pixel's luminance.
            if (SaturationBoost > 0f)
            {
                // Rec.601 luma in 0..255 units.
                float luma = 0.299f * r + 0.587f * g + 0.114f * b;
                r = luma + (r - luma) * (1f + SaturationBoost);
                g = luma + (g - luma) * (1f + SaturationBoost);
                b = luma + (b - luma) * (1f + SaturationBoost);
            }

            // Contrast: push values away from mid-grey (128).
            if (ContrastBoost > 0f)
            {
                float c = 1f + ContrastBoost;
                r = 128f + (r - 128f) * c;
                g = 128f + (g - 128f) * c;
                b = 128f + (b - 128f) * c;
            }

            // Clamp to 0..255.
            if (r < 0f) r = 0f; else if (r > 255f) r = 255f;
            if (g < 0f) g = 0f; else if (g > 255f) g = 255f;
            if (b < 0f) b = 0f; else if (b > 255f) b = 255f;

            rowF[idx + 0] = r;
            rowF[idx + 1] = g;
            rowF[idx + 2] = b;
        }

        /// <summary>
        /// Apply ordered Bayer dither to a float colour channel in the 0..255
        /// range, scaled to the active depth's quantisation step.
        /// At 16-bit the step is sub-LSB of the source, so this is effectively
        /// a no-op (dither self-disables when it cannot help).
        /// </summary>
        public static float ApplyDitherFloat(float value, int x, int y)
        {
            if (!DitherEnabled) return value;

            int bayer = BAYER_8X8[y & 7, x & 7];          // 0..63, mean 31.5
            // Quantisation step in 0..255 units = 255/(levels-1).
            float step = 255f / (Levels - 1);
            // Map the centered Bayer value (-0.5..+0.5 after /63 and -0.5) to
            // +/- half a quantisation step. This is the classic ordered-dither
            // error-feedback amplitude.
            float bias = (bayer - 31.5f) / 63f * step;
            float v = value + bias;
            if (v < 0f) v = 0f;
            else if (v > 255f) v = 255f;
            return v;
        }

        /// <summary>
        /// Apply RGB gamma correction to a float channel in the 0..255 range.
        /// gamma = 1.0 = no change. gamma &lt; 1 brightens (lifts dark signals),
        /// gamma &gt; 1 darkens (boosts contrast). Works for all colour schemes
        /// because it post-processes the final RGB value.
        /// </summary>
        public static float ApplyGammaFloat(float value)
        {
            if (Gamma == 1.0f) return value;
            double norm = value / 255.0;
            double corrected = 255.0 * Math.Pow(norm, Gamma);
            if (corrected < 0) corrected = 0;
            else if (corrected > 255) corrected = 255;
            return (float)corrected;
        }
    }
}
