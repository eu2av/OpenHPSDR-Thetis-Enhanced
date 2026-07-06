// Yurij-eu2av - 2026-07-04: Format-dependent waterfall pixel encoder.
//
// The waterfall color pipeline now works in float internally (0..255 per
// channel, RGBA order) and only quantises to the target DXGI surface format
// at the very end, in this class. Three output depths are supported:
//
//   8-bit  : Format.B8G8R8A8_UNorm   (256 levels/channel, classic)
//   10-bit : Format.R10G10B10A2_UNorm (1024 levels/channel, packed DWORD)
//   16-bit : Format.R16G16B16A16_Float (half-float, ~65536 levels/channel)
//
// EncodeRow() converts one scanline of float RGBA (rowF, always W*4 floats)
// into the packed byte layout expected by CopyFromMemory for the active
// depth. FillClearBuffer() writes an "opaque black" pattern for region clears.
//
// The active depth is selected in WaterfallEnhancer (Set/Get ColorDepth) and
// mirrored here via UpdateFormat(). The swap chain and D2D bitmaps are created
// with WaterfallPixelWriter.DxgiFormat, so changing the depth requires a full
// DX rebuild (handled by the restart-required flow in the Setup UI).

using System;
using SharpDX;
using SharpDX.DXGI;

// Yurij-eu2av - note on half-float packing: rather than depend on the
// SharpDX.Mathematics.Half internal RawValue accessor (which varies between
// SharpDX builds), we convert float -> IEEE-754 binary16 bits directly via
// FloatToHalfBits() below. This is dependency-free and bit-exact.

namespace Thetis
{
    public static class WaterfallPixelWriter
    {
        // ---- Active format state ---------------------------------------------
        // Yurij-eu2av - 2026-07-04: the renderer is now a Direct2D 1.1
        // DeviceContext (migrated from the legacy 1.0 RenderTarget), so the swap
        // chain and D2D bitmaps can use 10/16-bit back-buffer formats. The active
        // format follows WaterfallEnhancer.Depth: 8/10/16-bit.
        //
        // The internal colour pipeline works in float (all 8 schemes compute
        // colour in float, gamma/dither run in float), and dither scales its
        // delta to the output depth — so banding is reduced at every depth and
        // vanishes at 16-bit where the quantisation step is sub-perceptual.
        // quantisation step, and gamma is mathematically exact.
        public static int PixelSize { get; private set; } = 4;
        public static Format DxgiFormat { get; private set; } = Format.B8G8R8A8_UNorm;
        public static WaterfallEnhancer.ColorDepth Depth { get; private set; } = WaterfallEnhancer.ColorDepth.Bit8;

        /// <summary>
        /// Re-derive PixelSize / DxgiFormat from the active ColorDepth.
        /// Yurij-eu2av - 2026-07-04: now backed by a Direct2D 1.1 DeviceContext
        /// (not the legacy 1.0 RenderTarget), so 16-bit back-buffer formats are
        /// valid and honoured here. 10-bit (R10G10B10A2_UNorm) is NOT offered —
        /// Direct2D does not reliably support it as a bitmap surface across
        /// GPU drivers, even though D3D11 does. 16-bit half-float IS reliably
        /// supported by Direct2D and is strictly better (65536 vs 1024 levels).
        /// </summary>
        public static void UpdateFormat()
        {
            Depth = WaterfallEnhancer.Depth;
            switch (Depth)
            {
                default:
                case WaterfallEnhancer.ColorDepth.Bit8:
                    PixelSize = 4;
                    DxgiFormat = Format.B8G8R8A8_UNorm;
                    break;
                case WaterfallEnhancer.ColorDepth.Bit16:
                    PixelSize = 8;   // four half-floats
                    DxgiFormat = Format.R16G16B16A16_Float;
                    break;
            }
        }

        /// <summary>
        /// Encode one waterfall scanline from float RGBA into the packed byte
        /// layout for the active depth.
        ///
        /// rowF layout: [R,G,B,A] repeated, W*4 floats, values 0..255.
        /// row  layout: depth-dependent (BGRA / packed / half), PixelSize bytes/pixel.
        /// </summary>
        public static void EncodeRow(float[] rowF, byte[] row, int W)
        {
            switch (Depth)
            {
                case WaterfallEnhancer.ColorDepth.Bit8:
                    EncodeRow8(rowF, row, W);
                    break;
                case WaterfallEnhancer.ColorDepth.Bit10:
                    EncodeRow10(rowF, row, W);
                    break;
                case WaterfallEnhancer.ColorDepth.Bit16:
                    EncodeRow16(rowF, row, W);
                    break;
            }
        }

        // ---- 8-bit BGRA ------------------------------------------------------
        // Matches the original byte layout: B,G,R,A (premultiplied alpha is
        // handled by callers writing alpha=255; we round to nearest here).
        private static void EncodeRow8(float[] rowF, byte[] row, int W)
        {
            for (int px = 0; px < W; px++)
            {
                int fi = px * 4;
                int bi = px * 4;
                float r = ClampByte(rowF[fi + 0]);
                float g = ClampByte(rowF[fi + 1]);
                float b = ClampByte(rowF[fi + 2]);
                float a = ClampByte(rowF[fi + 3]);
                row[bi + 0] = FastRoundToByte(b);
                row[bi + 1] = FastRoundToByte(g);
                row[bi + 2] = FastRoundToByte(r);
                row[bi + 3] = FastRoundToByte(a);
            }
        }

        // ---- 10-bit packed R10G10B10A2 --------------------------------------
        // One 32-bit little-endian DWORD per pixel:
        //   bits  0..9  : R (0..1023)
        //   bits 10..19 : G (0..1023)
        //   bits 20..29 : B (0..1023)
        //   bits 30..31 : A (0..3)
        private static void EncodeRow10(float[] rowF, byte[] row, int W)
        {
            for (int px = 0; px < W; px++)
            {
                int fi = px * 4;
                int bi = px * 4;
                float r = ClampByte(rowF[fi + 0]);
                float g = ClampByte(rowF[fi + 1]);
                float b = ClampByte(rowF[fi + 2]);
                float a = ClampByte(rowF[fi + 3]);

                int r10 = RoundToInt(r * (1023f / 255f));
                int g10 = RoundToInt(g * (1023f / 255f));
                int b10 = RoundToInt(b * (1023f / 255f));
                int a2  = RoundToInt(a * (3f / 255f));

                if (r10 < 0) r10 = 0; else if (r10 > 1023) r10 = 1023;
                if (g10 < 0) g10 = 0; else if (g10 > 1023) g10 = 1023;
                if (b10 < 0) b10 = 0; else if (b10 > 1023) b10 = 1023;
                if (a2 < 0) a2 = 0; else if (a2 > 3) a2 = 3;

                uint packed = (uint)((r10) | (g10 << 10) | (b10 << 20) | (a2 << 30));
                // little-endian
                row[bi + 0] = (byte)(packed & 0xFF);
                row[bi + 1] = (byte)((packed >> 8) & 0xFF);
                row[bi + 2] = (byte)((packed >> 16) & 0xFF);
                row[bi + 3] = (byte)((packed >> 24) & 0xFF);
            }
        }

        // ---- 16-bit half-float RGBA -----------------------------------------
        // Four half-precision floats per pixel (R,G,B,A), each 2 bytes,
        // little-endian. Values are converted sRGB → linear before packing so
        // that D2D's LINEAR interpretation of the float surface reproduces the
        // same on-screen colour that 8-bit sRGB produced.
        private static void EncodeRow16(float[] rowF, byte[] row, int W)
        {
            for (int px = 0; px < W; px++)
            {
                int fi = px * 4;
                int bi = px * 8;
                float r = SrgbToLinear(ClampByte(rowF[fi + 0]) * (1f / 255f));
                float g = SrgbToLinear(ClampByte(rowF[fi + 1]) * (1f / 255f));
                float b = SrgbToLinear(ClampByte(rowF[fi + 2]) * (1f / 255f));
                float a = ClampByte(rowF[fi + 3]) * (1f / 255f); // alpha stays linear

                WriteHalf(row, bi + 0, r);
                WriteHalf(row, bi + 2, g);
                WriteHalf(row, bi + 4, b);
                WriteHalf(row, bi + 6, a);
            }
        }

        private static void WriteHalf(byte[] row, int offset, float value)
        {
            // Convert float -> IEEE-754 binary16 raw bits (little-endian on x86/x64).
            ushort bits = FloatToHalfBits(value);
            row[offset + 0] = (byte)(bits & 0xFF);
            row[offset + 1] = (byte)((bits >> 8) & 0xFF);
        }

        /// <summary>
        /// Convert a 32-bit float to IEEE-754 binary16 (half) raw bits.
        /// Handles denormals, overflow (-> Inf), NaN and underflow (-> 0).
        /// Bit-exact equivalent of the GPU's f32->f16 conversion.
        /// Yurij-eu2av - 2026-07-04: public wrapper so display.cs can pack
        /// background-image pixels to half-float on 16-bit surfaces.
        /// </summary>
        public static ushort FloatToHalfBitsPublic(float value)
        {
            return FloatToHalfBits(value);
        }

        /// <summary>
        /// Yurij-eu2av - 2026-07-04: sRGB → linear transfer function (IEC 61966-2-1).
        /// Direct2D interprets float values as LINEAR RGB, but 8-bit values as sRGB.
        /// So an 8-bit value of 128 (=0.5 sRGB) must become ~0.214 linear when
        /// packed into a 16-bit float surface, otherwise it renders brighter than
        /// it did at 8-bit. This is the standard sRGB EOTF inverse.
        /// </summary>
        public static float SrgbToLinear(float v)
        {
            if (v <= 0.04045f) return v / 12.92f;
            return (float)Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static unsafe ushort FloatToHalfBits(float value)
        {
            // reinterpret float as uint32
            uint f = *((uint*)&value);
            uint sign = (f >> 16) & 0x8000u;          // sign bit shifted to half position
            uint mantissa = f & 0x007FFFFFu;
            int exp = (int)((f >> 23) & 0xFFu) - 127 + 15; // rebase exponent

            if (exp <= 0)
            {
                // underflow or denormal: half denormals have a 10-bit mantissa
                if (exp < -10) return (ushort)sign;     // flush to zero
                mantissa |= 0x00800000u;                 // leading 1 from float mantissa
                uint shift = (uint)(14 - exp);
                uint hmant = mantissa >> (int)shift;
                return (ushort)(sign | hmant);
            }
            else if (exp == 0xFF - (127 - 15))
            {
                // Inf or NaN
                if (mantissa != 0) return (ushort)(sign | 0x7E00); // NaN
                return (ushort)(sign | 0x7C00);                     // Inf
            }
            else if (exp > 30)
            {
                // overflow to Inf
                return (ushort)(sign | 0x7C00);
            }

            return (ushort)(sign | ((uint)exp << 10) | (mantissa >> 13));
        }

        // ---- Opaque-black fill for region clears ----------------------------
        /// <summary>
        /// Fill a byte buffer with the "opaque black" pattern for the active
        /// depth. Used by clearWaterfallBitmapRegion(). buf length >= byteCount.
        /// </summary>
        public static void FillClearBuffer(byte[] buf, int byteCount)
        {
            switch (Depth)
            {
                case WaterfallEnhancer.ColorDepth.Bit8:
                {
                    // BGRA opaque black: B=0,G=0,R=0,A=255
                    for (int i = 0; i + 3 < byteCount; i += 4)
                    {
                        buf[i + 0] = 0;
                        buf[i + 1] = 0;
                        buf[i + 2] = 0;
                        buf[i + 3] = 255;
                    }
                    break;
                }
                case WaterfallEnhancer.ColorDepth.Bit10:
                {
                    // R10G10B10A2 opaque black: RGB=0, A=3 -> 0xC0000000
                    for (int i = 0; i + 3 < byteCount; i += 4)
                    {
                        buf[i + 0] = 0x00;
                        buf[i + 1] = 0x00;
                        buf[i + 2] = 0x00;
                        buf[i + 3] = 0xC0;
                    }
                    break;
                }
                case WaterfallEnhancer.ColorDepth.Bit16:
                {
                    // R16G16B16A16_FLOAT opaque black: R=G=B=0.0, A=1.0
                    // half(0.0)  = 0x0000
                    // half(1.0)  = 0x3C00
                    for (int i = 0; i + 7 < byteCount; i += 8)
                    {
                        buf[i + 0] = 0x00; buf[i + 1] = 0x00; // R = 0.0
                        buf[i + 2] = 0x00; buf[i + 3] = 0x00; // G = 0.0
                        buf[i + 4] = 0x00; buf[i + 5] = 0x00; // B = 0.0
                        buf[i + 6] = 0x00; buf[i + 7] = 0x3C; // A = 1.0
                    }
                    break;
                }
            }
        }

        // ---- Helpers --------------------------------------------------------
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static float ClampByte(float v)
        {
            if (v < 0f) return 0f;
            if (v > 255f) return 255f;
            return v;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static byte FastRoundToByte(float v)
        {
            // round-half-up to nearest byte; v already clamped to 0..255
            int i = (int)(v + 0.5f);
            if (i < 0) i = 0; else if (i > 255) i = 255;
            return (byte)i;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static int RoundToInt(float v)
        {
            return (int)(v + 0.5f);
        }
    }
}
