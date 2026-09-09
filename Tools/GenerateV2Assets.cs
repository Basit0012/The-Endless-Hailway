using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

public class GenerateV2Assets
{
    public static void Main(string[] args)
    {
        string projectDir = Directory.GetCurrentDirectory();
        string audioDir = Path.Combine(projectDir, "Assets/_Project/Audio");
        string paintDir = Path.Combine(projectDir, "Assets/_Project/Art/Textures/Paintings");

        if (!Directory.Exists(audioDir)) Directory.CreateDirectory(audioDir);
        if (!Directory.Exists(paintDir)) Directory.CreateDirectory(paintDir);

        Console.WriteLine("[GenerateV2Assets] Generating Audio Clips...");
        GenerateAudio(audioDir);

        Console.WriteLine("[GenerateV2Assets] Generating Painting Textures...");
        GeneratePaintings(paintDir);

        Console.WriteLine("[GenerateV2Assets] Complete!");
    }

    private static void GenerateAudio(string audioDir)
    {
        int sr = 44100;
        var rng = new Random(104);

        // 1. Heartbeat WAV (Lub-Dub, 1.0s loop)
        int hbTotal = (int)(sr * 1.0f);
        float[] hbSamples = new float[hbTotal];
        for (int i = 0; i < hbTotal; i++)
        {
            float t = (float)i / sr;
            float s = 0f;
            if (t >= 0.0f && t < 0.16f)
            {
                float env = (float)Math.Sin((t / 0.16f) * Math.PI);
                s += (float)Math.Sin(2.0 * Math.PI * 55.0 * t) * env * 0.95f;
                s += (float)Math.Sin(2.0 * Math.PI * 110.0 * t) * env * 0.3f;
            }
            if (t >= 0.22f && t < 0.36f)
            {
                float tRel = t - 0.22f;
                float env = (float)Math.Sin((tRel / 0.14f) * Math.PI);
                s += (float)Math.Sin(2.0 * Math.PI * 68.0 * t) * env * 0.75f;
                s += (float)Math.Sin(2.0 * Math.PI * 136.0 * t) * env * 0.25f;
            }
            hbSamples[i] = Clamp(s, -1f, 1f);
        }
        WriteWav(Path.Combine(audioDir, "sfx_heartbeat.wav"), hbSamples, sr);

        // 2. Ragged Breathing WAV (3.0s cycle)
        int brTotal = (int)(sr * 3.0f);
        float[] brSamples = new float[brTotal];
        for (int i = 0; i < brTotal; i++)
        {
            float t = (float)i / sr;
            float env = 0f;
            float centerFreq = 400f;
            if (t >= 0.2f && t < 1.3f)
            {
                env = (float)Math.Sin(((t - 0.2f) / 1.1f) * Math.PI) * 0.45f;
                centerFreq = 500f + (t - 0.2f) * 150f;
            }
            else if (t >= 1.5f && t < 2.8f)
            {
                env = (float)Math.Sin(((t - 1.5f) / 1.3f) * Math.PI) * 0.55f;
                centerFreq = 450f - (t - 1.5f) * 80f;
            }
            float whiteNoise = ((float)rng.NextDouble() * 2f - 1f);
            float formant = (float)Math.Sin(2.0 * Math.PI * centerFreq * t) * 0.25f;
            brSamples[i] = Clamp((whiteNoise * 0.75f + formant) * env, -1f, 1f);
        }
        WriteWav(Path.Combine(audioDir, "sfx_breathing.wav"), brSamples, sr);

        // 3. Ambient Whispers WAV (4.0s reversed/pitched murmurs)
        int whTotal = (int)(sr * 4.0f);
        float[] whSamples = new float[whTotal];
        for (int i = 0; i < whTotal; i++)
        {
            float t = (float)i / sr;
            float mod = (float)Math.Sin(2.0 * Math.PI * 2.5 * t) * 0.5f + 0.5f;
            float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.3f;
            float v1 = (float)Math.Sin(2.0 * Math.PI * (220.0 + Math.Sin(t * 3.0) * 40.0) * t) * 0.25f;
            float v2 = (float)Math.Sin(2.0 * Math.PI * 720.0 * t) * 0.15f;
            whSamples[i] = Clamp((noise + v1 + v2) * mod * 0.45f, -1f, 1f);
        }
        WriteWav(Path.Combine(audioDir, "amb_whispers.wav"), whSamples, sr);

        // 4. Jump Scare Stinger WAV (1.8s dissonant screech + sub impact)
        int jsTotal = (int)(sr * 1.8f);
        float[] jsSamples = new float[jsTotal];
        for (int i = 0; i < jsTotal; i++)
        {
            float t = (float)i / sr;
            float env = (float)Math.Exp(-t * 3.5f);
            float sub = (float)Math.Sin(2.0 * Math.PI * (80.0 - t * 25.0) * t) * 0.8f;
            float screech = ((float)Math.Sin(2.0 * Math.PI * 2400.0 * t) + (float)Math.Sin(2.0 * Math.PI * 3100.0 * t)) * 0.4f;
            float impact = ((float)rng.NextDouble() * 2f - 1f) * 0.6f;
            jsSamples[i] = Clamp((sub + screech + impact) * env, -1f, 1f);
        }
        WriteWav(Path.Combine(audioDir, "sfx_jumpscare.wav"), jsSamples, sr);
    }

    private static void GeneratePaintings(string paintDir)
    {
        int size = 512;

        for (int stage = 0; stage < 5; stage++)
        {
            using (Bitmap bmp = new Bitmap(size, size))
            {
                var rng = new Random(1337 + stage * 71);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float u = (float)x / size;
                        float v = (float)(size - 1 - y) / size; // Y up
                        float r = 0f, g = 0f, b = 0f;

                        float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.04f;

                        if (stage == 0) // Desolate Landscape
                        {
                            if (v > 0.42f)
                            {
                                float skyT = (v - 0.42f) / 0.58f;
                                r = Lerp(0.86f, 0.70f, skyT);
                                g = Lerp(0.83f, 0.72f, skyT);
                                b = Lerp(0.70f, 0.65f, skyT);
                            }
                            else
                            {
                                float groundT = v / 0.42f;
                                float hill = (float)Math.Sin(u * 5.0) * 0.04f;
                                float gt = Clamp(groundT + hill, 0f, 1f);
                                r = Lerp(0.20f, 0.38f, gt);
                                g = Lerp(0.22f, 0.36f, gt);
                                b = Lerp(0.14f, 0.22f, gt);
                            }

                            if (u >= 0.46f && u <= 0.54f && v >= 0.40f && v <= 0.48f)
                            {
                                r = 0.18f; g = 0.15f; b = 0.12f;
                            }
                            if (v > 0.48f && v <= 0.53f)
                            {
                                float roofHalfW = (0.53f - v) * 0.8f;
                                if (Math.Abs(u - 0.50f) <= roofHalfW) { r = 0.14f; g = 0.11f; b = 0.09f; }
                            }
                        }
                        else if (stage == 1) // Subtle Wrongness
                        {
                            if (v > 0.42f)
                            {
                                float skyT = (v - 0.42f) / 0.58f;
                                r = Lerp(0.75f, 0.55f, skyT);
                                g = Lerp(0.82f, 0.65f, skyT);
                                b = Lerp(0.72f, 0.60f, skyT);
                            }
                            else
                            {
                                float groundT = v / 0.42f;
                                r = Lerp(0.15f, 0.28f, groundT);
                                g = Lerp(0.18f, 0.30f, groundT);
                                b = Lerp(0.15f, 0.20f, groundT);
                            }

                            if (u >= 0.46f && u <= 0.54f && v >= 0.40f && v <= 0.48f)
                            {
                                r = 0.12f; g = 0.10f; b = 0.09f;
                                if ((u >= 0.475f && u <= 0.495f && v >= 0.43f && v <= 0.46f) ||
                                    (u >= 0.505f && u <= 0.525f && v >= 0.43f && v <= 0.46f))
                                {
                                    r = 0f; g = 0f; b = 0f;
                                    if (u >= 0.482f && u <= 0.488f && v >= 0.442f && v <= 0.448f)
                                    {
                                        r = 0.85f; g = 0.85f; b = 0.82f;
                                    }
                                }
                            }
                            if (v > 0.48f && v <= 0.53f)
                            {
                                float roofHalfW = (0.53f - v) * 0.8f;
                                if (Math.Abs(u - 0.50f) <= roofHalfW) { r = 0.10f; g = 0.08f; b = 0.07f; }
                            }
                        }
                        else if (stage == 2) // Dollhouse Room with Porcelain Doll
                        {
                            if (v > 0.35f)
                            {
                                if (v > 0.50f)
                                {
                                    float stripe = Math.Sin(u * 80.0) > 0.7 ? 0.05f : 0f;
                                    r = 0.82f + stripe; g = 0.75f + stripe; b = 0.45f;
                                }
                                else
                                {
                                    float panel = ((u * 12f) % 1f < 0.08f) ? -0.1f : 0f;
                                    r = 0.14f + panel; g = 0.32f + panel; b = 0.35f;
                                }
                            }
                            else
                            {
                                float perspY = (0.35f - v) / 0.35f;
                                float checkX = (u - 0.5f) / (1f - perspY * 0.7f) * 8f;
                                float checkY = 1f / (perspY + 0.15f) * 1.5f;
                                bool isWhite = ((int)Math.Floor(checkX) + (int)Math.Floor(checkY)) % 2 == 0;
                                r = isWhite ? 0.82f : 0.08f;
                                g = isWhite ? 0.80f : 0.10f;
                                b = isWhite ? 0.74f : 0.12f;
                            }

                            if (u >= 0.42f && u <= 0.58f && v >= 0.20f && v <= 0.60f)
                            {
                                if (Math.Abs(u - 0.50f) > 0.05f || (v >= 0.32f && v <= 0.36f))
                                {
                                    r = 0.25f; g = 0.14f; b = 0.08f;
                                }
                            }

                            float dx = (u - 0.50f) * 1.1f;
                            float dy = (v - 0.54f);
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (dist < 0.09f)
                            {
                                r = 0.94f; g = 0.92f; b = 0.88f;
                                if (Math.Abs(dx) > 0.035f && dy < 0.01f && dy > -0.04f)
                                {
                                    r = Lerp(r, 0.95f, 0.55f);
                                    g = Lerp(g, 0.45f, 0.55f);
                                    b = Lerp(b, 0.45f, 0.55f);
                                }
                                if ((Math.Abs(dx - 0.035f) < 0.015f || Math.Abs(dx + 0.035f) < 0.015f) && Math.Abs(dy - 0.02f) < 0.02f)
                                {
                                    r = 0.05f; g = 0.08f; b = 0.12f;
                                    if (dx > 0 && dy > 0.025f) { r = 1f; g = 1f; b = 1f; }
                                }
                                if (Math.Abs(dx) < 0.012f && dy < -0.045f && dy > -0.06f)
                                {
                                    r = 0.85f; g = 0.2f; b = 0.25f;
                                }
                            }
                        }
                        else if (stage == 3) // Grotesque Melt
                        {
                            if (v > 0.35f)
                            {
                                r = (v > 0.50f) ? 0.65f : 0.08f;
                                g = (v > 0.50f) ? 0.60f : 0.22f;
                                b = (v > 0.50f) ? 0.25f : 0.24f;
                                if (Math.Sin(u * 25.0) * Math.Cos(v * 15.0) > 0.45) { r = 0.04f; g = 0.04f; b = 0.04f; }
                            }
                            else
                            {
                                r = 0.06f; g = 0.08f; b = 0.09f;
                            }

                            float dx = (u - 0.50f) * 1.3f;
                            float dy = (v - 0.52f);
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (dist < 0.11f)
                            {
                                r = 0.88f; g = 0.85f; b = 0.78f;
                                if ((Math.Abs(dx - 0.038f) < 0.022f || Math.Abs(dx + 0.038f) < 0.022f) && dy > 0f && dy < 0.05f)
                                {
                                    r = 0f; g = 0f; b = 0f;
                                }
                                if ((Math.Abs(dx - 0.038f) < 0.012f || Math.Abs(dx + 0.038f) < 0.012f) && dy <= 0f && dy > -0.12f)
                                {
                                    r = 0f; g = 0f; b = 0f;
                                }
                                if (Math.Abs(dx) < 0.022f && dy < -0.02f && dy > -0.09f)
                                {
                                    r = 0f; g = 0f; b = 0f;
                                }
                            }
                            if (u < 0.1f || u > 0.9f)
                            {
                                r = Lerp(r, 0.8f, 0.4f);
                                g = Lerp(g, 0.1f, 0.4f);
                                b = Lerp(b, 0.2f, 0.4f);
                            }
                        }
                        else // Stage 4: Full Nightmare
                        {
                            float corridorDepth = Math.Abs(u - 0.5f) * 2f;
                            float fireGlow = (float)(Math.Sin(u * 12.0) * Math.Cos(v * 10.0) * 0.5 + 0.5);
                            r = Lerp(0.08f, Lerp(0.95f, 0.45f, v), (1f - corridorDepth * 0.7f) * fireGlow);
                            g = Lerp(0.04f, Lerp(0.40f, 0.08f, v), (1f - corridorDepth * 0.7f) * fireGlow);
                            b = Lerp(0.04f, Lerp(0.08f, 0.04f, v), (1f - corridorDepth * 0.7f) * fireGlow);

                            if (Math.Abs(u - 0.5f) < 0.12f && v > 0.35f && v < 0.75f)
                            {
                                r = Lerp(r, 1.0f, 0.85f);
                                g = Lerp(g, 0.85f, 0.85f);
                                b = Lerp(b, 0.35f, 0.85f);
                            }

                            float bodyX = Math.Abs(u - 0.5f);
                            float headDist = (float)Math.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.68f) * (v - 0.68f));
                            bool isHead = headDist < 0.065f;
                            bool isTorso = bodyX < 0.14f && v >= 0.20f && v <= 0.65f;
                            bool isArms = bodyX < (0.28f - (v - 0.2f) * 0.3f) && v >= 0.15f && v <= 0.50f;

                            if (isHead || isTorso || isArms)
                            {
                                r = 0.02f; g = 0.02f; b = 0.03f;
                            }

                            if (rng.NextDouble() < 0.015)
                            {
                                r = 1.0f; g = 0.7f; b = 0.2f;
                            }
                        }

                        // Edge vignette and noise
                        float edgeDist = Math.Min(Math.Min(u, 1f - u), Math.Min(v, 1f - v));
                        float vig = Clamp(edgeDist * 10f, 0f, 1f);
                        r = Clamp(r + noise, 0f, 1f);
                        g = Clamp(g + noise, 0f, 1f);
                        b = Clamp(b + noise, 0f, 1f);
                        r = Lerp(0.05f, r, vig);
                        g = Lerp(0.05f, g, vig);
                        b = Lerp(0.05f, b, vig);

                        int ir = (int)(r * 255);
                        int ig = (int)(g * 255);
                        int ib = (int)(b * 255);
                        bmp.SetPixel(x, y, Color.FromArgb(255, ir, ig, ib));
                    }
                }

                string pngPath = Path.Combine(paintDir, string.Format("T_Painting_Stage{0}.png", stage));
                bmp.Save(pngPath, ImageFormat.Png);
            }
        }
    }

    private static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
    private static float Clamp(float v, float min, float max) { return v < min ? min : (v > max ? max : v); }

    private static void WriteWav(string filePath, float[] samples, int sampleRate)
    {
        using (var fs = new FileStream(filePath, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * 2;
            int dataSize = samples.Length * 2;
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);
            for (int i = 0; i < samples.Length; i++)
            {
                short val = (short)(Clamp(samples[i], -1f, 1f) * 32767);
                bw.Write(val);
            }
        }
    }
}
