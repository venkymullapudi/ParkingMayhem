using UnityEngine;
using System.Collections.Generic;

// Manages all in-game audio using procedurally generated clips (no audio assets needed).
// Engine sound pitch is reactive to vehicle speed; ambient changes per terrain.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // Volume knobs (can be wired to Settings UI)
    [Range(0f, 1f)] public float masterVolume = 0.85f;
    [Range(0f, 1f)] public float sfxVolume    = 0.90f;
    [Range(0f, 1f)] public float ambientVolume = 0.32f;
    [Range(0f, 1f)] public float engineVolume  = 0.60f;

    // Audio sources (created at runtime, not serialised)
    private AudioSource _engine;   // looping, pitch-reactive to speed
    private AudioSource _ambient;  // looping terrain atmosphere
    private AudioSource _sfx;      // one-shot gameplay effects
    private AudioSource _ui;       // one-shot UI sounds (separate so SFX never cuts clicks)

    // Generated clip libraries
    private Dictionary<GameManager.VehicleKind, AudioClip> _engineClips;
    private Dictionary<GameManager.TerrainType, AudioClip> _ambientClips;
    private AudioClip _collisionClip;
    private AudioClip _successClip;
    private AudioClip _levelCompleteClip;
    private AudioClip _buttonClickClip;
    private AudioClip _timerWarningClip;
    private AudioClip _timeBonusClip;
    private AudioClip _countdownBeepClip;
    private AudioClip _countdownGoClip;

    // Engine pitch envelope per vehicle kind
    private float _idlePitch = 0.80f;
    private float _maxPitch  = 2.10f;
    private float _targetPitch = 0.80f;
    private float _targetEngVol = 0f;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _engine  = AddSource(loop: true,  vol: 0f);
        _ambient = AddSource(loop: true,  vol: 0f);
        _sfx     = AddSource(loop: false, vol: 1f);
        _ui      = AddSource(loop: false, vol: 1f);

        GenerateClips();

        // Restore persisted mute preference (global, via AudioListener).
        Muted = PlayerPrefs.GetInt("Muted", 0) == 1;
        AudioListener.volume = Muted ? 0f : 1f;

        // Restore saved volume settings (previously written by the Settings sliders but never read
        // back, so user volumes reset every launch).
        masterVolume = PlayerPrefs.GetFloat("MusicVolume", masterVolume);
        sfxVolume    = PlayerPrefs.GetFloat("SFXVolume",   sfxVolume);

        // UI clicks should still be audible while the pause menu freezes game audio.
        _ui.ignoreListenerPause = true;
    }

    // Freeze/unfreeze game audio with the pause menu (AudioSources ignore timeScale, so the
    // engine loop otherwise drones on under the pause panel).
    public void SetGamePaused(bool paused) { AudioListener.pause = paused; }

    // ── Global mute (persisted) ────────────────────────────────────────────────
    public bool Muted { get; private set; }
    public void SetMuted(bool m)
    {
        Muted = m;
        AudioListener.volume = m ? 0f : 1f;
        PlayerPrefs.SetInt("Muted", m ? 1 : 0);
        PlayerPrefs.Save();
    }
    public bool ToggleMute() { SetMuted(!Muted); return Muted; }

    private AudioSource AddSource(bool loop, float vol)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.loop          = loop;
        src.volume        = vol;
        src.spatialBlend  = 0f;   // 2D — no positional falloff
        src.playOnAwake   = false;
        return src;
    }

    // Generates all clips on startup (~2 MB total, runs once)
    private void GenerateClips()
    {
        _engineClips = new Dictionary<GameManager.VehicleKind, AudioClip>();
        foreach (GameManager.VehicleKind k in System.Enum.GetValues(typeof(GameManager.VehicleKind)))
            _engineClips[k] = ProceduralAudio.CreateEngineClip(k);

        _ambientClips = new Dictionary<GameManager.TerrainType, AudioClip>();
        foreach (GameManager.TerrainType t in System.Enum.GetValues(typeof(GameManager.TerrainType)))
            _ambientClips[t] = ProceduralAudio.CreateTerrainAmbient(t);

        _collisionClip     = ProceduralAudio.CreateCollisionClip();
        _successClip       = ProceduralAudio.CreateSuccessClip();
        _levelCompleteClip = ProceduralAudio.CreateLevelCompleteClip();
        _buttonClickClip   = ProceduralAudio.CreateButtonClickClip();
        _timerWarningClip  = ProceduralAudio.CreateTimerWarningClip();
        _timeBonusClip     = ProceduralAudio.CreateTimeBonusClip();
        _countdownBeepClip = ProceduralAudio.CreateCountdownBeepClip(false);
        _countdownGoClip   = ProceduralAudio.CreateCountdownBeepClip(true);
    }

    private void Update()
    {
        // Smoothly interpolate engine pitch and volume every frame (allocation-free lerp toward
        // the speed-driven target). Null-guard: _engine may be Unity fake-null if destroyed.
        if (_engine == null || !_engine.isPlaying) return;
        _engine.pitch  = Mathf.Lerp(_engine.pitch,  _targetPitch,  Time.deltaTime * 5f);
        _engine.volume = Mathf.Lerp(_engine.volume, _targetEngVol, Time.deltaTime * 4f);
    }

    // ── Called by GameManager.BootstrapGameScene ──────────────────────────────

    public void StartLevel(GameManager.TerrainType terrain, GameManager.VehicleKind kind, int level)
    {
        // Terrain ambient
        if (_ambientClips.TryGetValue(terrain, out var amb))
        {
            _ambient.clip   = amb;
            _ambient.volume = ambientVolume * masterVolume;
            if (!_ambient.isPlaying) _ambient.Play();
            else { _ambient.Stop(); _ambient.Play(); }
        }

        // Vehicle engine
        ConfigureEnginePitch(kind);
        if (_engineClips.TryGetValue(kind, out var eng))
        {
            _engine.clip   = eng;
            _engine.pitch  = _idlePitch;
            _engine.volume = 0f;
            _targetEngVol  = engineVolume * masterVolume * 0.5f; // fade up from idle
            if (!_engine.isPlaying) _engine.Play();
            else { _engine.Stop(); _engine.Play(); }
        }

        // Level-start chime (unique pitch per level). Destroy the previous bake first — a new
        // native AudioClip per level/retry was an unbounded memory leak over long sessions.
        if (_lastLevelChime != null) Destroy(_lastLevelChime);
        _lastLevelChime = ProceduralAudio.CreateLevelStartClip(level);
        _ui.PlayOneShot(_lastLevelChime, sfxVolume * masterVolume);
    }
    private AudioClip _lastLevelChime;

    // Fail sting + silence the engine so it doesn't drone behind the game-over panel.
    public void PlayGameOverSound()
    {
        _targetEngVol   = 0f;
        if (_engine  != null) _engine.volume  = 0f;
        if (_ambient != null) _ambient.volume = ambientVolume * masterVolume * 0.15f;
        if (_timerWarningClip == null) return;
        // Two descending low-pitched warning tones read as a "fail" sting without a new clip bake.
        var tmp = gameObject.AddComponent<AudioSource>();
        tmp.spatialBlend = 0f; tmp.playOnAwake = false;
        tmp.pitch = 0.62f;
        tmp.PlayOneShot(_timerWarningClip, 0.9f * masterVolume);
        Destroy(tmp, _timerWarningClip.length / 0.62f + 0.2f);
    }

    // Sets the idle / max pitch range that suits the vehicle class
    private void ConfigureEnginePitch(GameManager.VehicleKind kind)
    {
        switch (kind)
        {
            case GameManager.VehicleKind.Ship:
            case GameManager.VehicleKind.Rocket:
            case GameManager.VehicleKind.Shuttle:
                _idlePitch = 0.65f; _maxPitch = 1.60f; break;
            case GameManager.VehicleKind.Satellite:
                _idlePitch = 0.90f; _maxPitch = 1.30f; break;
            case GameManager.VehicleKind.Helicopter:
                _idlePitch = 0.82f; _maxPitch = 1.50f; break;
            case GameManager.VehicleKind.Plane:
            case GameManager.VehicleKind.Jet:
                _idlePitch = 0.88f; _maxPitch = 2.20f; break;
            case GameManager.VehicleKind.Truck:
            case GameManager.VehicleKind.Bus:
                _idlePitch = 0.72f; _maxPitch = 1.80f; break;
            default:
                _idlePitch = 0.80f; _maxPitch = 2.10f; break;
        }
        _targetPitch = _idlePitch;
    }

    // ── Called by VehicleController.UpdateSpeed each frame ────────────────────

    // normSpeed: current speed / maxSpeed (0 idle → 1 top speed)
    // throttle : movement.y value (-1 full reverse … +1 full forward)
    public void UpdateEngineState(float normSpeed, float throttle)
    {
        float speed = Mathf.Clamp01(normSpeed);
        _targetPitch = Mathf.Lerp(_idlePitch, _maxPitch, speed * 0.68f);

        // Engine is louder under load (throttle applied) and at speed
        float loadBoost = Mathf.Abs(throttle) > 0.05f ? 0.12f : 0f;
        _targetEngVol = Mathf.Clamp(
            (0.30f + speed * 0.35f + loadBoost) * engineVolume * masterVolume,
            0.10f * masterVolume, engineVolume * masterVolume);
    }

    // ── SFX API (called by VehicleController / UIManager / ParkingZone) ───────

    public void PlayCollisionSound()
    {
        _sfx.PlayOneShot(_collisionClip, sfxVolume * masterVolume);
        // Silence engine momentarily — crash stops the car
        _targetEngVol = 0f;
        _engine.volume = 0f;
    }

    public void PlayParkingSuccessSound() => PlayParkingSuccessSound(0);

    // Combo feedback: the success chime rises in pitch with the win streak. Uses a short-lived
    // temp source so the pitch applies cleanly without affecting other SFX.
    public void PlayParkingSuccessSound(int streak)
    {
        if (_successClip == null) return;
        var tmp = gameObject.AddComponent<AudioSource>();
        tmp.spatialBlend = 0f;
        tmp.playOnAwake  = false;
        tmp.pitch        = 1f + Mathf.Min(Mathf.Max(streak - 1, 0), 8) * 0.06f;
        tmp.PlayOneShot(_successClip, sfxVolume * masterVolume);
        Destroy(tmp, _successClip.length / Mathf.Max(tmp.pitch, 0.1f) + 0.2f);
    }

    // Rising-pitch ping for each star revealed on the level-complete screen (index 0..2).
    public void PlayStarChime(int index)
    {
        if (_timeBonusClip == null) return;
        var tmp = gameObject.AddComponent<AudioSource>();
        tmp.spatialBlend = 0f;
        tmp.playOnAwake  = false;
        tmp.pitch        = 1.0f + Mathf.Clamp(index, 0, 4) * 0.18f;
        tmp.PlayOneShot(_timeBonusClip, 0.85f * masterVolume);
        Destroy(tmp, _timeBonusClip.length / Mathf.Max(tmp.pitch, 0.1f) + 0.2f);
    }

    public void PlayLevelCompleteSound()
    {
        _sfx.PlayOneShot(_levelCompleteClip, sfxVolume * masterVolume);
        // Fade out engine and ambient during results
        _targetEngVol   = 0f;
        _ambient.volume = ambientVolume * masterVolume * 0.20f;
    }

    public void PlayButtonClickSound()
    {
        _ui.PlayOneShot(_buttonClickClip, 0.55f * masterVolume);
    }

    public void PlayTimerWarning()
    {
        _ui.PlayOneShot(_timerWarningClip, 0.65f * masterVolume);
    }

    public void PlayTimeBonusSound()
    {
        _ui.PlayOneShot(_timeBonusClip, 0.80f * masterVolume);
    }

    public void PlayCountdownBeep(bool isGo)
    {
        var clip = isGo ? _countdownGoClip : _countdownBeepClip;
        _ui.PlayOneShot(clip, 0.75f * masterVolume);
    }

    // ── Legacy stubs (preserves call sites from original AudioManager) ────────

    public void PlayVehicleMoveSound() { }
    public void PlaySound(string _)    { }
    public void StopSound(string _)    { }
    public void SetMusicVolume(float v) { masterVolume = Mathf.Clamp01(v); }
    public void SetSFXVolume(float v)   { sfxVolume    = Mathf.Clamp01(v); }
}
