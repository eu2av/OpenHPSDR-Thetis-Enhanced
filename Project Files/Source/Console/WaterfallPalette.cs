// Yurij-eu2av - 2026-07-04: Multi-stop gradient interpolator for waterfall
// colour schemes. Replaces the hard if/else bands of the legacy schemes
// (enhanced, SPECTRAN, LinRad) with a precomputed 256-entry float LUT that
// is linearly interpolated from a set of control points (stops).
//
// This is the same approach used by SDR Console and Spectrum Lab: define a
// handful of "key colours" at positions 0..1, and let the interpolator fill
// in the 256 intermediate steps. The result is a visually smooth, saturated
// gradient — no banding, no hard transitions.
//
// Adding a new palette is just a new Stop[] array: no per-pixel logic needed.

using System;

namespace Thetis
{
    public class WaterfallPalette
    {
        /// <summary>
        /// A control point: signal position (0..1) → RGB colour (0..255).
        /// </summary>
        public struct Stop
        {
            public readonly float Pos;
            public readonly float R, G, B;
            public Stop(float pos, float r, float g, float b)
            {
                Pos = pos; R = r; G = g; B = b;
            }
            public Stop(float pos, int r, int g, int b)
            {
                Pos = pos; R = r; G = g; B = b;
            }
        }

        private const int LUT_SIZE = 256;
        // 256 entries × 3 channels (R,G,B), each 0..255 float.
        private readonly float[] _lut = new float[LUT_SIZE * 3];
        private Stop[] _stops;

        /// <summary>
        /// Build the 256-entry LUT from the given control points.
        /// Stops must be sorted ascending by Pos, with Pos in 0..1.
        /// </summary>
        public void Build(Stop[] stops)
        {
            _stops = stops;
            if (stops == null || stops.Length == 0) return;

            for (int i = 0; i < LUT_SIZE; i++)
            {
                float pos = (float)i / (LUT_SIZE - 1);
                SampleStops(stops, pos, out float r, out float g, out float b);
                _lut[i * 3 + 0] = r;
                _lut[i * 3 + 1] = g;
                _lut[i * 3 + 2] = b;
            }
        }

        /// <summary>
        /// Look up the colour for a signal percentage (0..1).
        /// O(1): direct index into the precomputed LUT.
        /// </summary>
        public void Sample(float percent, out float r, out float g, out float b)
        {
            // Clamp + map to LUT index (0..255).
            if (percent <= 0f) percent = 0f;
            else if (percent >= 1f) percent = 1f;

            int idx = (int)(percent * (LUT_SIZE - 1));
            int o = idx * 3;
            r = _lut[o + 0];
            g = _lut[o + 1];
            b = _lut[o + 2];
        }

        // ---- Stop interpolation ------------------------------------------------
        // Find the two stops bracketing 'pos' and linearly interpolate between them.
        private static void SampleStops(Stop[] stops, float pos,
            out float r, out float g, out float b)
        {
            // Before first stop or at/after last stop → clamp to endpoint colour.
            if (pos <= stops[0].Pos)
            {
                r = stops[0].R; g = stops[0].G; b = stops[0].B;
                return;
            }
            int last = stops.Length - 1;
            if (pos >= stops[last].Pos)
            {
                r = stops[last].R; g = stops[last].G; b = stops[last].B;
                return;
            }

            // Find bracketing pair.
            for (int i = 0; i < last; i++)
            {
                if (pos >= stops[i].Pos && pos <= stops[i + 1].Pos)
                {
                    float span = stops[i + 1].Pos - stops[i].Pos;
                    float t = span > 0f ? (pos - stops[i].Pos) / span : 0f;
                    r = stops[i].R + (stops[i + 1].R - stops[i].R) * t;
                    g = stops[i].G + (stops[i + 1].G - stops[i].G) * t;
                    b = stops[i].B + (stops[i + 1].B - stops[i].B) * t;
                    return;
                }
            }

            // Fallback (should not reach here).
            r = stops[last].R; g = stops[last].G; b = stops[last].B;
        }

        // ---- Ready-made palettes ----------------------------------------------

        /// <summary>
        /// "Console" palette — SDR Console style. Saturated, smooth gradient
        /// optimised for readability: dark background for weak signals, bright
        /// colours for strong. Black→navy→blue→cyan→green→yellow→orange→red→white.
        /// </summary>
        public static Stop[] ConsoleStops => new Stop[]
        {
            new Stop(0.00f,   0,   0,   0),    // black
            new Stop(0.12f,   0,   0,  90),    // navy
            new Stop(0.25f,   0,  20, 200),    // deep blue
            new Stop(0.38f,   0, 120, 230),    // blue
            new Stop(0.48f,   0, 200, 200),    // cyan-teal
            new Stop(0.55f,  40, 220,  60),    // green
            new Stop(0.65f, 200, 230,   0),    // yellow-green
            new Stop(0.72f, 255, 220,   0),    // yellow
            new Stop(0.80f, 255, 150,   0),    // orange
            new Stop(0.88f, 245,  50,  20),    // red
            new Stop(0.95f, 255, 140, 180),    // pink
            new Stop(1.00f, 255, 255, 255),    // white
        };

        /// <summary>
        /// "Thermal" palette — heat-map / Inferno style. Warm spectrum that
        /// gives excellent contrast for weak signals on a dark background.
        /// Black→indigo→purple→magenta→red→orange→yellow→white.
        /// </summary>
        public static Stop[] ThermalStops => new Stop[]
        {
            new Stop(0.00f,   0,   0,   0),    // black
            new Stop(0.15f,  20,   0,  50),    // near-black indigo
            new Stop(0.30f,  55,  10, 100),    // indigo
            new Stop(0.45f, 110,  10, 120),    // purple
            new Stop(0.58f, 175,  30,  90),    // magenta
            new Stop(0.70f, 220,  70,  30),    // red
            new Stop(0.82f, 250, 140,   0),    // orange
            new Stop(0.92f, 255, 215,  60),    // yellow
            new Stop(1.00f, 255, 255, 220),    // warm white
        };

        /// <summary>
        /// "DeepBlue" palette — a cool, high-contrast variant with a strong
        /// dark-to-bright blue ramp. Good for low-light operating environments.
        /// </summary>
        public static Stop[] DeepBlueStops => new Stop[]
        {
            new Stop(0.00f,   0,   0,   0),    // black
            new Stop(0.20f,   0,  10,  40),    // midnight blue
            new Stop(0.40f,   0,  40, 120),    // dark blue
            new Stop(0.55f,   0, 110, 200),    // blue
            new Stop(0.68f,   0, 190, 220),    // cyan
            new Stop(0.80f, 120, 235, 200),    // mint
            new Stop(0.90f, 220, 250, 180),    // pale yellow-green
            new Stop(1.00f, 255, 255, 255),    // white
        };
    }
}
