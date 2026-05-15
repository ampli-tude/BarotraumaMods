#if CLIENT
using Barotrauma.Sounds;

namespace CyclopsVoices;

internal static class AudioManager
{
    private const float VOICE_VOLUME = 1.0f;
    private const float ALERT_VOLUME = 1.0f;
    private const float FADE_RATE    = 0.004f;  // gain change per frame (~4 s full fade at 60 fps)

    // eventType -> one or more sounds (random pick when multiple)
    private static readonly Dictionary<string, Sound[]> _sounds = new();

    // Voice FIFO queue — one line plays at a time
    private static readonly Queue<Sound> _queue = new();
    private static SoundChannel? _current = null;

    // Encounter music (independent loop, fade in/out)
    // OST crossfade is not attempted — SoundManager exposes no MusicVolume setter in C#;
    // our music simply layers on top of the vanilla OST.
    private static Sound?        _musicSound   = null;
    private static SoundChannel? _musicChannel = null;
    private static float         _currentGain  = 0f;
    private static float         _targetGain   = 0f;
    private static bool          _disposing    = false;

    public static void Initialize(string modDir)
    {
        string voiceDir  = System.IO.Path.Combine(modDir, "audio", "CyclopsVoices");
        string musicFile = System.IO.Path.Combine(modDir, "audio", "Abandon Ship.ogg");

        // Pool of two for hull_breach (random pick)
        LoadPool("hull_breach",      voiceDir, "hull_breach.ogg", "AI_external_damage.ogg");
        LoadOne("leak_fix",          voiceDir, "leak_fix.ogg");
        LoadOne("creature_attack",   voiceDir, "creature_attack.ogg");
        LoadOne("fire",              voiceDir, "fire_detected.ogg");
        LoadOne("fire_extinguished", voiceDir, "AI_fire_extinguished.ogg");
        LoadOne("oxygen",            voiceDir, "oxy_off.ogg");
        LoadOne("flood_mild",        voiceDir, "AI_hull_low.ogg");
        LoadOne("flood_severe",      voiceDir, "AI_hull_crit.ogg");

        try
        {
            _musicSound = GameMain.SoundManager?.LoadSound(musicFile, stream: true);
        }
        catch (Exception e)
        {
            Plugin.Log($"[CyclopsVoices] ERROR loading encounter music: {e.Message}");
        }
    }

    private static void LoadOne(string eventType, string dir, string filename)
        => LoadPool(eventType, dir, filename);

    private static void LoadPool(string eventType, string dir, params string[] filenames)
    {
        var list = new List<Sound>();
        foreach (string filename in filenames)
        {
            try
            {
                string path  = System.IO.Path.Combine(dir, filename);
                Sound? sound = GameMain.SoundManager?.LoadSound(path, stream: false);
                if (sound != null) list.Add(sound);
            }
            catch (Exception e)
            {
                Plugin.Log($"[CyclopsVoices] ERROR loading {filename}: {e.Message}");
            }
        }
        if (list.Count > 0) _sounds[eventType] = list.ToArray();
    }

    // Called from NetBridge when a network message arrives.
    public static void OnEvent(string eventType, string action)
    {
        if (action == "music_start")
            StartMusic();
        else if (action == "music_stop")
            StopMusic();
        else
            EnqueueByName(eventType);
    }

    private static void EnqueueByName(string eventType)
    {
        if (!_sounds.TryGetValue(eventType, out Sound[]? pool) || pool.Length == 0) return;
        Sound sound = pool.Length == 1 ? pool[0] : pool[Random.Shared.Next(pool.Length)];
        _queue.Enqueue(sound);
    }

    // ── Encounter music ────────────────────────────────────────────────────────

    private static void StartMusic()
    {
        if (_musicSound == null) return;
        if (_musicChannel == null || !_musicChannel.IsPlaying)
        {
            try
            {
                _musicChannel = _musicSound.Play(ALERT_VOLUME);
                _musicChannel.Gain = 0f;
                _currentGain = 0f;
            }
            catch { return; }
        }
        _targetGain = 1f;
        _disposing  = false;
    }

    private static void StopMusic()
    {
        _targetGain = 0f;
        _disposing  = true;
    }

    // ── Per-frame tick ─────────────────────────────────────────────────────────

    public static void Tick()
    {
        TickQueue();
        TickMusicFade();
    }

    private static bool IsPlayerInsideSub()
    {
        var c = Character.Controlled;
        return c != null && c.Submarine == Submarine.MainSub;
    }

    private static bool IsPlayerSubmergedInSub()
    {
        var c = Character.Controlled;
        return c != null && c.Submarine == Submarine.MainSub && c.AnimController?.InWater == true;
    }

    private static void TickQueue()
    {
        if (_current != null)
        {
            try
            {
                if (_current.IsPlaying)
                {
                    _current.Muffled = IsPlayerSubmergedInSub();
                    return;
                }
            }
            catch { }
            _current = null;
        }

        if (_queue.Count == 0) return;
        if (!IsPlayerInsideSub()) return;  // hold queue until player is back inside

        Sound next = _queue.Dequeue();
        try
        {
            _current = next.Play(VOICE_VOLUME);
            _current.Muffled = IsPlayerSubmergedInSub();
        }
        catch { }
    }

    private static void TickMusicFade()
    {
        if (_musicChannel == null && !_disposing) return;

        if (_currentGain != _targetGain)
        {
            float delta = _targetGain - _currentGain;
            _currentGain += Math.Abs(delta) <= FADE_RATE ? delta : Math.Sign(delta) * FADE_RATE;

            if (_musicChannel != null)
                try { _musicChannel.Gain = _currentGain; } catch { }
        }

        if (_disposing && _currentGain <= 0f)
        {
            if (_musicChannel != null)
            {
                try { _musicChannel.FadeOutAndDispose(); } catch { }
                _musicChannel = null;
            }
            _disposing = false;
        }
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    // Stop playback and clear queue, but keep loaded sounds (used between rounds).
    public static void Reset()
    {
        _queue.Clear();
        if (_current != null)
        {
            try { _current.FadeOutAndDispose(); } catch { }
            _current = null;
        }
        StopMusic();
    }

    // Full cleanup on plugin dispose.
    public static void Dispose()
    {
        Reset();
        foreach (Sound[] pool in _sounds.Values)
            foreach (Sound s in pool)
                try { s.Dispose(); } catch { }
        _sounds.Clear();
        if (_musicSound != null)
        {
            try { _musicSound.Dispose(); } catch { }
            _musicSound = null;
        }
    }
}
#endif
