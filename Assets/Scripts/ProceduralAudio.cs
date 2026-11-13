using UnityEngine;

// Generates AudioClip objects entirely from math — no audio asset files required.
// All clips are mono, 22050 Hz. Integer-Hz sine frequencies give seamless 1-second loops.
public static class ProceduralAudio
{
    private const int SR = 22050; // sample rate

    // ── Engine clips (1-second seamless loop, pitch-shifted at runtime) ──────────

    public static AudioClip CreateEngineClip(GameManager.VehicleKind kind)
    {
        float[] d = new float[SR];
        switch (kind)
        {
            case GameManager.VehicleKind.Truck:     FillTruckEngine(d);     break;
            case GameManager.VehicleKind.Bus:       FillBusEngine(d);       break;
            case GameManager.VehicleKind.Boat:      FillBoatEngine(d);      break;
            case GameManager.VehicleKind.Yacht:     FillYachtEngine(d);     break;
            case GameManager.VehicleKind.Ship:      FillShipEngine(d);      break;
            case GameManager.VehicleKind.Plane:     FillPlaneEngine(d);     break;
            case GameManager.VehicleKind.Jet:       FillJetEngine(d);       break;
            case GameManager.VehicleKind.Helicopter:FillHelicopterEngine(d);break;
            case GameManager.VehicleKind.Rocket:
            case GameManager.VehicleKind.Shuttle:   FillRocketEngine(d);    break;
            case GameManager.VehicleKind.Satellite: FillSatelliteEngine(d); break;
            default:                                FillCarEngine(d);       break;
        }
        Normalize(d, 0.72f);
        return Bake($"Eng_{kind}", d, SR, loop: true);
    }

    private static void FillCarEngine(float[] d)
    {
        // V4 petrol: 80 Hz fundamental + harmonics, tiny noise grain
        var r = new System.Random(11);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = Sin(80, t) * 0.60f
                 + Sin(160, t) * 0.22f
                 + Sin(240, t) * 0.10f
                 + Sin(320, t) * 0.05f
                 + (float)(r.NextDouble() * 2 - 1) * 0.020f;
        }
    }

    private static void FillTruckEngine(float[] d)
    {
        // Diesel V8: 55 Hz, strong odd harmonics, rhythmic chug
        var r = new System.Random(22);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float chug = 0.82f + 0.18f * Sin(55 / 2, t); // half-speed pulse = diesel fire rhythm
            d[i] = (Sin(55, t) * 0.50f
                  + Sin(165, t) * 0.24f
                  + Sin(275, t) * 0.14f
                  + Sin(385, t) * 0.07f
                  + (float)(r.NextDouble() * 2 - 1) * 0.040f) * chug;
        }
    }

    private static void FillBusEngine(float[] d)
    {
        var r = new System.Random(33);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = Sin(62, t) * 0.55f
                 + Sin(124, t) * 0.22f
                 + Sin(186, t) * 0.12f
                 + Sin(248, t) * 0.06f
                 + (float)(r.NextDouble() * 2 - 1) * 0.030f;
        }
    }

    private static void FillBoatEngine(float[] d)
    {
        // Outboard: 45 Hz, rhythmic pulse like water-chopped propeller
        var r = new System.Random(44);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float pulse = 0.78f + 0.22f * Sin(45 * 4, t);
            d[i] = (Sin(45, t) * 0.52f
                  + Sin(90, t) * 0.28f
                  + Sin(135, t) * 0.12f
                  + (float)(r.NextDouble() * 2 - 1) * 0.050f) * pulse;
        }
    }

    private static void FillYachtEngine(float[] d)
    {
        var r = new System.Random(55);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = Sin(70, t) * 0.42f
                 + Sin(140, t) * 0.25f
                 + Sin(210, t) * 0.10f
                 + (float)(r.NextDouble() * 2 - 1) * 0.065f; // more wind noise
        }
    }

    private static void FillShipEngine(float[] d)
    {
        // Massive slow-turning diesel: 28 Hz, deep and heavy
        var r = new System.Random(66);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = Sin(28, t) * 0.60f
                 + Sin(56, t) * 0.22f
                 + Sin(84, t) * 0.10f
                 + Sin(112, t) * 0.05f
                 + (float)(r.NextDouble() * 2 - 1) * 0.020f;
        }
    }

    private static void FillPlaneEngine(float[] d)
    {
        // Turboprop: 450 Hz turbine whine + sub for body
        var r = new System.Random(77);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = Sin(450, t) * 0.40f
                 + Sin(225, t) * 0.22f   // sub-harmonic for mass
                 + Sin(900, t) * 0.14f
                 + (float)(r.NextDouble() * 2 - 1) * 0.080f;
        }
    }

    private static void FillJetEngine(float[] d)
    {
        // High-bypass turbofan: higher pitch, dominated by noise (exhaust roar)
        var r = new System.Random(88);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = Sin(800, t) * 0.22f
                 + Sin(1200, t) * 0.10f
                 + (float)(r.NextDouble() * 2 - 1) * 0.55f;   // jets = mostly noise
        }
    }

    private static void FillHelicopterEngine(float[] d)
    {
        // 12 Hz rotor chop × 300 Hz turbine gives distinctive "whop-whop"
        var r = new System.Random(99);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float rotor = 0.42f + 0.58f * Mathf.Max(0f, Sin(12, t)); // 0..1 blade thump
            float turbine = Sin(300, t) * 0.45f
                          + Sin(600, t) * 0.15f
                          + (float)(r.NextDouble() * 2 - 1) * 0.10f;
            d[i] = turbine * rotor;
        }
    }

    private static void FillRocketEngine(float[] d)
    {
        // Deep combustion roar: 35 Hz rumble + heavy broadband noise
        var r = new System.Random(110);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = Sin(35, t) * 0.28f
                 + Sin(70, t) * 0.12f
                 + (float)(r.NextDouble() * 2 - 1) * 0.68f;  // rockets are mostly noise
        }
    }

    private static void FillSatelliteEngine(float[] d)
    {
        // Cold-gas thrusters: 220 Hz tone with a 2 Hz on/off pulse (thruster firing rhythm)
        var r = new System.Random(121);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float pulse = Mathf.Max(0f, Sin(2, t));  // 0..1 firing window
            d[i] = (Sin(220, t) * 0.50f
                  + (float)(r.NextDouble() * 2 - 1) * 0.18f) * pulse;
        }
    }

    // ── Terrain ambient clips (4-second loop) ─────────────────────────────────

    public static AudioClip CreateTerrainAmbient(GameManager.TerrainType terrain)
    {
        int n = SR * 4;
        float[] d = new float[n];
        switch (terrain)
        {
            case GameManager.TerrainType.Road:  FillRoadAmbient(d);  break;
            case GameManager.TerrainType.Water: FillWaterAmbient(d); break;
            case GameManager.TerrainType.Air:   FillAirAmbient(d);   break;
            case GameManager.TerrainType.Space: FillSpaceAmbient(d); break;
        }
        Normalize(d, 0.55f);
        ApplyLoopFade(d, 441); // 20 ms crossfade at loop point prevents click
        return Bake($"Ambient_{terrain}", d, SR, loop: true);
    }

    private static void FillRoadAmbient(float[] d)
    {
        // Urban traffic: low-pass noise (distant engine rumble) + car-pass sweeps
        var r = new System.Random(200);
        float lp = 0f;
        double phase = 0;   // accumulated phase: freq changes stay click-free (abs-time phase jumped)
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float noise = (float)(r.NextDouble() * 2 - 1);
            lp = Mathf.Lerp(lp, noise, 0.018f); // ~200 Hz low-pass
            // Periodic car passing: slow frequency sweep, phase-accumulated to avoid zipper clicks
            float freq = 200f + 60f * Mathf.Sin(t * 0.4f);
            phase += 2.0 * Mathf.PI * freq / SR;
            float pass = Mathf.Sin((float)phase)
                       * Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(t * 0.28f)) * 0.18f;
            d[i] = lp * 0.60f + pass;
        }
    }

    private static void FillWaterAmbient(float[] d)
    {
        // Ocean: surf noise + slow wave swell (1 and 2 Hz for perfect 4-second loop)
        var r = new System.Random(300);
        float lp = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float noise = (float)(r.NextDouble() * 2 - 1);
            lp = Mathf.Lerp(lp, noise, 0.012f);
            // Wave swell envelope: 1 Hz = 4 cycles in 4 s → perfect loop
            float swell = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 1f * t));
            d[i] = lp * swell * 0.80f;
        }
    }

    private static void FillAirAmbient(float[] d)
    {
        // Wind: band-pass filtered noise with slowly gusting amplitude
        var r = new System.Random(400);
        float lp = 0f, prev = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float noise = (float)(r.NextDouble() * 2 - 1);
            lp = Mathf.Lerp(lp, noise, 0.035f);
            float hp = lp - prev; prev = lp; // first-difference ≈ high-pass
            // Gust envelope: modulate amplitude with a slow wave
            float gust = 0.40f + 0.60f * (0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 0.25f * t));
            d[i] = hp * gust * 9f;
        }
    }

    private static void FillSpaceAmbient(float[] d)
    {
        // Deep space: faint electronic drone (40 + 60 Hz = even multiples → seamless 4-second loop)
        var r = new System.Random(500);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float drone = Sin(40, t) * 0.18f + Sin(60, t) * 0.09f;
            // Very rare cosmic "tick" (sub-audible but felt)
            float tick = r.NextDouble() < 0.00008 ? (float)(r.NextDouble() * 2 - 1) * 0.25f : 0f;
            d[i] = drone + tick;
        }
    }

    // ── One-shot SFX ──────────────────────────────────────────────────────────

    public static AudioClip CreateCollisionClip()
    {
        int n = (int)(SR * 0.55f);
        float[] d = new float[n];
        var r = new System.Random(600);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float env = Mathf.Exp(-(float)i / n * 9f);
            float noise = (float)(r.NextDouble() * 2 - 1);
            lp = Mathf.Lerp(lp, noise, 0.09f);
            float thud = Sin(55, (float)i / SR) * Mathf.Exp(-(float)i / n * 16f);
            d[i] = (lp * env * 0.70f + thud * 0.55f);
        }
        Normalize(d, 0.90f);
        return Bake("Collision", d, SR, loop: false);
    }

    public static AudioClip CreateSuccessClip()
    {
        // C5 → E5 → G5 → C6 arpeggio
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        int n = (int)(SR * 0.55f);
        float[] d = new float[n];
        float noteDur = (float)n / SR / notes.Length;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            int ni = Mathf.Min((int)(t / noteDur), notes.Length - 1);
            float nt = (t - ni * noteDur) / noteDur;
            float env = Mathf.Exp(-nt * 5f) * Mathf.Min(1f, nt * 40f);
            d[i] = Mathf.Sin(2f * Mathf.PI * notes[ni] * t) * env * 0.58f;
        }
        Normalize(d, 0.80f);
        return Bake("Success", d, SR, loop: false);
    }

    public static AudioClip CreateLevelCompleteClip()
    {
        // C major triad swell + upper octave (C4-E4-G4-C5)
        float[] chord = { 261.63f, 329.63f, 392f, 523.25f };
        int n = SR; // 1 second
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Min(1f, t * 5f) * Mathf.Exp(-Mathf.Max(0f, t - 0.6f) * 2.5f);
            float s = 0f;
            foreach (float f in chord) s += Mathf.Sin(2f * Mathf.PI * f * t);
            d[i] = s / chord.Length * env * 0.65f;
        }
        Normalize(d, 0.85f);
        return Bake("LevelComplete", d, SR, loop: false);
    }

    public static AudioClip CreateButtonClickClip()
    {
        int n = (int)(SR * 0.065f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float env = Mathf.Exp(-(float)i / n * 22f);
            d[i] = Sin(1800, (float)i / SR) * env * 0.50f;
        }
        return Bake("Click", d, SR, loop: false);
    }

    public static AudioClip CreateLevelStartClip(int level)
    {
        // Two-note ascending chime; pitch rises gently with level (capped at A5)
        float baseFreq = Mathf.Min(440f + (level - 1) * 4f, 880f);
        float hiFreq = baseFreq * 1.25f; // major third above
        int n = (int)(SR * 0.35f);
        float[] d = new float[n];
        int half = n / 2;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float freq = (i < half) ? baseFreq : hiFreq;
            float localT = (i < half) ? (float)i / half : (float)(i - half) / (n - half);
            float env = Mathf.Exp(-localT * 4.5f) * Mathf.Min(1f, localT * 35f);
            d[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.55f;
        }
        Normalize(d, 0.72f);
        return Bake($"LvlStart_{level}", d, SR, loop: false);
    }

    public static AudioClip CreateTimerWarningClip()
    {
        int n = (int)(SR * 0.12f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float env = Mathf.Exp(-(float)i / n * 10f);
            d[i] = Sin(880, (float)i / SR) * env * 0.40f;
        }
        return Bake("TimerWarn", d, SR, loop: false);
    }

    // Countdown beep: a sharp 440 Hz tick for 3/2/1, a bright chord for GO.
    public static AudioClip CreateCountdownBeepClip(bool isGo)
    {
        if (isGo)
        {
            // GO: bright 880+1320 Hz major chord, short attack, quick decay
            int n = (int)(SR * 0.35f);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t   = (float)i / SR;
                float env = Mathf.Exp(-t * 5.0f);
                d[i] = (Sin(880, t) * 0.35f + Sin(1100, t) * 0.28f + Sin(1320, t) * 0.22f) * env;
            }
            Normalize(d, 0.70f);
            return Bake("CountdownGo", d, SR, loop: false);
        }
        else
        {
            // tick: 660 Hz short ping
            int n = (int)(SR * 0.18f);
            float[] d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                d[i] = Sin(660, t) * Mathf.Exp(-t * 18f) * 0.55f;
            }
            Normalize(d, 0.55f);
            return Bake("CountdownBeep", d, SR, loop: false);
        }
    }

    // Bright ascending two-tone chime — plays when collecting a +5s token.
    public static AudioClip CreateTimeBonusClip()
    {
        int n = (int)(SR * 0.40f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t   = (float)i / SR;
            float env = Mathf.Exp(-t * 6.5f);
            // Two-note ascend: 880 Hz then 1320 Hz (perfect fifth up). The second note's time
            // base restarts at the boundary so its phase starts at 0 — the old absolute-time
            // phase jumped mid-envelope and produced an audible click at t=0.15.
            float note = t < 0.15f
                ? Sin(880,  t) * 0.50f
                : Sin(1320, t - 0.15f) * 0.50f;
            d[i] = note * env;
        }
        Normalize(d, 0.65f);
        return Bake("TimeBonus", d, SR, loop: false);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    // Integer-Hz sine → always phase-aligned at t=0 and t=1s (seamless loop guarantee)
    private static float Sin(int freqHz, float t) => Mathf.Sin(2f * Mathf.PI * freqHz * t);

    private static void Normalize(float[] d, float targetPeak)
    {
        float peak = 0f;
        foreach (float s in d) if (Mathf.Abs(s) > peak) peak = Mathf.Abs(s);
        if (peak < 0.0001f) return;
        float scale = targetPeak / peak;
        for (int i = 0; i < d.Length; i++) d[i] *= scale;
    }

    // Fade in + fade out at loop boundaries to prevent click at the loop point
    private static void ApplyLoopFade(float[] d, int fadeLen)
    {
        int len = Mathf.Min(fadeLen, d.Length / 2);
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / len;
            d[i] *= t;
            d[d.Length - 1 - i] *= t;
        }
    }

    private static AudioClip Bake(string name, float[] data, int sampleRate, bool loop)
    {
        var clip = AudioClip.Create(name, data.Length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
