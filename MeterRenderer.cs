using System;
using UnityEngine;

namespace BetterHitErrorMeter
{
    /// <summary>
    /// Runtime texture generation for straight and curved hit error meters.
    /// One-time cost per size/shape change, zero per-frame CPU.
    /// </summary>
    public static class MeterRenderer
    {
        // --- CAD-measured geometry (SVG 400×200 coordinates) ---
        private const float SvgW = 400f;
        private const float SvgH = 200f;
        private const float SvgCenterX = 200f;
        private const float SvgCenterY = 200f; // bottom edge center

        // Straight meter rects (y=68..132 area, h=64)
        private const float S_BlackX = 35f, S_BlackY = 68f, S_BlackW = 330f, S_BlackH = 64f;
        private const float S_GreenX = 43f, S_GreenY = 94f, S_GreenW = 314f, S_GreenH = 12f;
        private const float S_YellowLX = 88f, S_YellowRX = 275f, S_YellowY = 94f, S_YellowW = 37f, S_YellowH = 12f;
        private const float S_OrangeLX = 50f, S_OrangeRX = 312f, S_OrangeY = 94f, S_OrangeW = 38f, S_OrangeH = 12f;
        private const float S_RedLX = 43f, S_RedRX = 350f, S_RedY = 94f, S_RedW = 7f, S_RedH = 12f;
        private const float S_NeedleX = 200f, S_NeedleY1 = 92f, S_NeedleY2 = 108f, S_NeedleW = 4f;

        // Curved meter ring radii
        private const float C_GrayInner = 118f, C_GrayOuter = 183f;
        private const float C_BandInner = 146.5f, C_BandOuter = 159f;

        // Curved meter angular zones (SVG degrees: 0=right, CW)
        private const float AngGrayL = 204f, AngGrayR = 336f;
        private const float AngRedL = 207f, AngRedR = 333f;
        private const float AngOrangeL = 210f, AngOrangeR = 330f;
        private const float AngYellowL = 225f, AngYellowR = 315f;
        private const float AngGreenL = 240f, AngGreenR = 300f;
        private const float AngCenter = 270f;

        // Needle
        private const float C_NeedleY1 = 39f, C_NeedleY2 = 55f, C_NeedleHalfW = 2f;

        // --- Colors ---
        private static readonly Color32 ColBlack = new Color32(0, 0, 0, 85);
        private static readonly Color32 ColGray = new Color32(170, 170, 170, 255);
        private static readonly Color32 ColGreen = new Color32(0x5F, 0xFF, 0x4E, 255);
        private static readonly Color32 ColYellow = new Color32(0xFC, 0xFF, 0x4D, 255);
        private static readonly Color32 ColOrange = new Color32(0xFF, 0x6F, 0x4D, 255);
        private static readonly Color32 ColRed = new Color32(0xFF, 0x00, 0x00, 255);
        private static readonly Color32 ColWhite = new Color32(0xF8, 0xF8, 0xF8, 255);
        private static readonly Color32 ColTransparent = new Color32(0, 0, 0, 0);

        public static Texture2D GenerateStraightMeter(float scale)
        {
            int w = Mathf.RoundToInt(SvgW * scale);
            int h = Mathf.RoundToInt(SvgH * scale);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = ColTransparent;

            float s = scale;

            // Layer 1: black background
            FillRect(pixels, w, S_BlackX * s, S_BlackY * s, S_BlackW * s, S_BlackH * s, ColBlack);

            // Layer 2: green base
            FillRect(pixels, w, S_GreenX * s, S_GreenY * s, S_GreenW * s, S_GreenH * s, ColGreen);

            // Layer 3: orange
            FillRect(pixels, w, S_OrangeLX * s, S_OrangeY * s, S_OrangeW * s, S_OrangeH * s, ColOrange);
            FillRect(pixels, w, S_OrangeRX * s, S_OrangeY * s, S_OrangeW * s, S_OrangeH * s, ColOrange);

            // Layer 4: yellow (on top of orange)
            FillRect(pixels, w, S_YellowLX * s, S_YellowY * s, S_YellowW * s, S_YellowH * s, ColYellow);
            FillRect(pixels, w, S_YellowRX * s, S_YellowY * s, S_YellowW * s, S_YellowH * s, ColYellow);

            // Layer 5: red (on top of orange edges)
            FillRect(pixels, w, S_RedLX * s, S_RedY * s, S_RedW * s, S_RedH * s, ColRed);
            FillRect(pixels, w, S_RedRX * s, S_RedY * s, S_RedW * s, S_RedH * s, ColRed);

            // Layer 6: white needle
            float nx = S_NeedleX * s - S_NeedleW * s / 2f;
            FillRect(pixels, w, nx, S_NeedleY1 * s, S_NeedleW * s, (S_NeedleY2 - S_NeedleY1) * s, ColWhite);

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D GenerateCurvedMeter(float scale)
        {
            int w = Mathf.RoundToInt(SvgW * scale);
            int h = Mathf.RoundToInt(SvgH * scale);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = ColTransparent;

            float s = scale;
            float cx = SvgCenterX * s;
            float cy = SvgCenterY * s;

            // Pre-scale radii
            float grayInner = C_GrayInner * s;
            float grayOuter = C_GrayOuter * s;
            float bandInner = C_BandInner * s;
            float bandOuter = C_BandOuter * s;

            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    float dx = px - cx;
                    float dy = py - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Outside ring entirely
                    if (dist < grayInner || dist > grayOuter)
                        continue;

                    // Angle in SVG convention (0=right, CW)
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (ang < 0f) ang += 360f;

                    int idx = py * w + px;

                    // Gray background (full arc 204°→336°)
                    if (ang >= AngGrayL && ang <= AngGrayR)
                    {
                        // Check if within colored band
                        if (dist >= bandInner && dist <= bandOuter)
                        {
                            // Determine color zone
                            if (ang >= AngGreenL && ang <= AngGreenR)
                                pixels[idx] = ColGreen;
                            else if (ang >= AngYellowL && ang <= AngYellowR)
                                pixels[idx] = ColYellow;
                            else if (ang >= AngOrangeL && ang <= AngOrangeR)
                                pixels[idx] = ColOrange;
                            else if (ang >= AngRedL && ang <= AngRedR)
                                pixels[idx] = ColRed;
                            else
                                pixels[idx] = ColGray;
                        }
                        else
                        {
                            pixels[idx] = ColGray;
                        }

                        // Check needle (vertical line near center angle)
                        float needleX = cx;
                        float ndy = needleX - px;
                        float ndy2 = py - C_NeedleY1 * s;
                        float ndy3 = C_NeedleY2 * s - py;
                        if (Mathf.Abs(ndy) <= C_NeedleHalfW * s &&
                            py >= C_NeedleY1 * s && py <= C_NeedleY2 * s)
                        {
                            pixels[idx] = ColWhite;
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static void FillRect(Color32[] pixels, int texWidth,
            float x, float y, float w, float h, Color32 color)
        {
            int ix = Mathf.Max(0, Mathf.RoundToInt(x));
            int iy = Mathf.Max(0, Mathf.RoundToInt(y));
            int iw = Mathf.RoundToInt(w);
            int ih = Mathf.RoundToInt(h);

            for (int row = iy; row < iy + ih && row < 2000; row++) // safety bound
            {
                int baseIdx = row * texWidth;
                for (int col = ix; col < ix + iw && col < 4000; col++)
                {
                    if (col >= 0 && col < texWidth && row >= 0 && row < texWidth) // rough bound
                        pixels[baseIdx + col] = color;
                }
            }
        }
    }
}
