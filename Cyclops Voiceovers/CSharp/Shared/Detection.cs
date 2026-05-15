#if SERVER
namespace CyclopsVoices;

internal static class Detection
{
    // Detection constants
    private const float  CREATURE_RANGE          = 500f;
    private const double BREACH_COOLDOWN         = 15.0;
    private const double CREATURE_COOLDOWN       = 30.0;
    private const double FIRE_COOLDOWN           = 20.0;
    private const float  LOW_OXY_THRESHOLD       = 95f;
    private const float  RECOVER_OXY_THRESHOLD   = 97f;
    private const float  FLOOD_LOW_THRESHOLD     = 0.15f;
    private const float  FLOOD_CRIT_THRESHOLD    = 0.30f;
    private const float  DETECT_RANGE            = 10000f;
    private const double TRIGGER_CHANCE          = 0.20;
    private const float  BOSS_VITALITY_THRESHOLD = 2000f;

    // Hull breach
    private static bool   _wasBreached      = false;
    private static double _lastBreachTime   = -BREACH_COOLDOWN;
    private static double _lastCreatureTime = -CREATURE_COOLDOWN;

    // Fire
    private static bool   _wasOnFire      = false;
    private static double _lastFireTime   = -FIRE_COOLDOWN;

    // Oxygen
    private static bool _oxyWasOffline = false;

    // Flood
    private static bool _wasFloodingLow  = false;
    private static bool _wasFloodingCrit = false;

    // Encounter music
    private static bool _musicPlaying       = false;
    private static bool _rolledForEncounter = false;
    private static bool _encounterActive    = false;
    private static bool _triggered          = false;
    private static bool _lastInRange        = false;

    public static bool EncounterMusicPlaying => _musicPlaying;

    public static void Check(Submarine mainSub)
    {
        CheckHullBreach(mainSub);
        CheckEncounterMusic(mainSub);
        CheckFire(mainSub);
        CheckOxygen(mainSub);
        CheckFlood(mainSub);
    }

    // ── Hull breach ────────────────────────────────────────────────────────────

    private static void CheckHullBreach(Submarine mainSub)
    {
        int  count       = 0;
        bool creatureNear = false;

        foreach (var gap in Gap.GapList)
        {
            try
            {
                if (gap.Submarine != mainSub) continue;
                if (gap.IsRoomToRoom)          continue;
                if (gap.Open <= 0f)            continue;
                if (gap.ConnectedDoor != null) continue;

                count++;

                if (!creatureNear)
                {
                    foreach (var c in Character.CharacterList)
                    {
                        try
                        {
                            if (c.IsHuman || c.IsDead) continue;
                            if (Vector2.Distance(c.WorldPosition, gap.WorldPosition) < CREATURE_RANGE)
                            {
                                creatureNear = true;
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        if (count > 0 && !_wasBreached)
        {
            _wasBreached = true;

            if (creatureNear && Timing.TotalTime - _lastCreatureTime >= CREATURE_COOLDOWN)
            {
                _lastCreatureTime = Timing.TotalTime;
                NetBridge.Send("creature_attack");
                Plugin.Log("[CyclopsVoices] Creature attack on sub.");
            }

            if (Timing.TotalTime - _lastBreachTime >= BREACH_COOLDOWN)
            {
                _lastBreachTime = Timing.TotalTime;
                NetBridge.Send("hull_breach");
                Plugin.Log("[CyclopsVoices] Hull breach detected.");
            }
        }
        else if (count == 0 && _wasBreached)
        {
            _wasBreached = false;
            NetBridge.Send("leak_fix");
            Plugin.Log("[CyclopsVoices] All breaches sealed.");
        }
    }

    // ── Fire ───────────────────────────────────────────────────────────────────

    private static void CheckFire(Submarine mainSub)
    {
        bool fireActive = false;
        foreach (var hull in Hull.HullList)
        {
            try
            {
                if (hull.Submarine != mainSub) continue;
                foreach (var fs in hull.FireSources)
                {
                    if (fs != null) { fireActive = true; break; }
                }
                if (fireActive) break;
            }
            catch { }
        }

        if (fireActive && !_wasOnFire)
        {
            _wasOnFire = true;
            if (Timing.TotalTime - _lastFireTime >= FIRE_COOLDOWN)
            {
                _lastFireTime = Timing.TotalTime;
                NetBridge.Send("fire");
                Plugin.Log("[CyclopsVoices] Fire detected.");
            }
        }
        else if (!fireActive && _wasOnFire)
        {
            _wasOnFire = false;
            NetBridge.Send("fire_extinguished");
            Plugin.Log("[CyclopsVoices] Fire extinguished.");
        }
    }

    // ── Oxygen ─────────────────────────────────────────────────────────────────

    private static void CheckOxygen(Submarine mainSub)
    {
        float total = 0f;
        int   count = 0;
        foreach (var hull in Hull.HullList)
        {
            try
            {
                if (hull.Submarine != mainSub) continue;
                total += hull.OxygenPercentage;
                count++;
            }
            catch { }
        }

        if (count == 0) return;
        float avg = total / count;

        if (!_oxyWasOffline && avg < LOW_OXY_THRESHOLD)
        {
            _oxyWasOffline = true;
            NetBridge.Send("oxygen");
            Plugin.Log("[CyclopsVoices] Oxygen low — generator offline.");
        }
        else if (_oxyWasOffline && avg >= RECOVER_OXY_THRESHOLD)
        {
            _oxyWasOffline = false;
        }
    }

    // ── Flood ──────────────────────────────────────────────────────────────────

    private static void CheckFlood(Submarine mainSub)
    {
        float totalWater = 0f, totalCap = 0f;
        foreach (var hull in Hull.HullList)
        {
            try
            {
                if (hull.Submarine != mainSub) continue;
                totalWater += hull.WaterVolume;
                totalCap   += hull.Volume;
            }
            catch { }
        }

        if (totalCap <= 0f) return;
        float ratio = totalWater / totalCap;

        if (ratio >= FLOOD_CRIT_THRESHOLD)
        {
            if (!_wasFloodingCrit)
            {
                _wasFloodingCrit = true;
                _wasFloodingLow  = true;
                NetBridge.Send("flood_severe");
                Plugin.Log("[CyclopsVoices] Hull critical — buoyancy failing.");
            }
        }
        else if (ratio >= FLOOD_LOW_THRESHOLD)
        {
            if (!_wasFloodingLow)
            {
                _wasFloodingLow = true;
                NetBridge.Send("flood_mild");
                Plugin.Log("[CyclopsVoices] Hull integrity low.");
            }
        }
        else
        {
            _wasFloodingLow  = false;
            _wasFloodingCrit = false;
        }
    }

    // ── Encounter music ────────────────────────────────────────────────────────

    private static void CheckEncounterMusic(Submarine mainSub)
    {
        bool exists  = AnyTargetExists(mainSub);
        bool inRange = exists && AnyTargetInRange(mainSub);

        if (!exists)
        {
            if (_encounterActive)
            {
                _encounterActive    = false;
                _rolledForEncounter = false;
                _lastInRange        = false;
                if (_triggered)
                {
                    _triggered = false;
                    if (_musicPlaying)
                    {
                        _musicPlaying = false;
                        NetBridge.Send("encounter_music", "music_stop");
                        Plugin.Log("[CyclopsVoices] Encounter ended, stopping music.");
                    }
                }
            }
            return;
        }

        _encounterActive = true;

        if (inRange && !_rolledForEncounter)
        {
            _rolledForEncounter = true;
            if (Random.Shared.NextDouble() < TRIGGER_CHANCE)
            {
                _triggered    = true;
                _musicPlaying = true;
                _lastInRange  = true;
                NetBridge.Send("encounter_music", "music_start");
                Plugin.Log("[CyclopsVoices] Encounter music triggered.");
            }
        }

        if (_triggered)
        {
            if (inRange && !_lastInRange)
            {
                _lastInRange  = true;
                _musicPlaying = true;
                NetBridge.Send("encounter_music", "music_start");
            }
            else if (!inRange && _lastInRange)
            {
                _lastInRange  = false;
                _musicPlaying = false;
                NetBridge.Send("encounter_music", "music_stop");
            }
        }
    }

    private static bool SubHasLivingCrew(Submarine sub)
    {
        foreach (var c in Character.CharacterList)
        {
            try { if (!c.IsDead && c.Submarine == sub) return true; }
            catch { }
        }
        return false;
    }

    private static bool AnyBossExists()
    {
        foreach (var c in Character.CharacterList)
        {
            try { if (!c.IsHuman && !c.IsDead && c.MaxVitality >= BOSS_VITALITY_THRESHOLD) return true; }
            catch { }
        }
        return false;
    }

    private static bool AnyEnemySubExists(Submarine mainSub)
    {
        foreach (var sub in Submarine.Loaded)
        {
            try
            {
                if (sub == mainSub) continue;
                if (sub.Info.IsOutpost || sub.Info.IsWreck || sub.Info.IsBeacon) continue;
                if (SubHasLivingCrew(sub)) return true;
            }
            catch { }
        }
        return false;
    }

    private static bool AnyTargetExists(Submarine mainSub)
        => AnyBossExists() || AnyEnemySubExists(mainSub);

    private static bool BossInRange(Submarine mainSub)
    {
        foreach (var c in Character.CharacterList)
        {
            try
            {
                if (!c.IsHuman && !c.IsDead && c.MaxVitality >= BOSS_VITALITY_THRESHOLD)
                    if (Vector2.Distance(c.WorldPosition, mainSub.WorldPosition) < DETECT_RANGE)
                        return true;
            }
            catch { }
        }
        return false;
    }

    private static bool EnemySubInRange(Submarine mainSub)
    {
        foreach (var sub in Submarine.Loaded)
        {
            try
            {
                if (sub == mainSub) continue;
                if (sub.Info.IsOutpost || sub.Info.IsWreck || sub.Info.IsBeacon) continue;
                if (SubHasLivingCrew(sub) && Vector2.Distance(sub.WorldPosition, mainSub.WorldPosition) < DETECT_RANGE)
                    return true;
            }
            catch { }
        }
        return false;
    }

    private static bool AnyTargetInRange(Submarine mainSub)
        => BossInRange(mainSub) || EnemySubInRange(mainSub);

    // Called when a new client connects — catch them up if music is already playing.
    public static void OnPlayerConnected(Client client)
    {
        if (!_musicPlaying) return;
        try
        {
            NetBridge.SendToConnection("encounter_music", "music_start", client.Connection);
            Plugin.Log("[CyclopsVoices] Sent music_start to connecting client.");
        }
        catch (Exception e)
        {
            Plugin.Log($"[CyclopsVoices] playerConnected error: {e.Message}");
        }
    }

    public static void Reset()
    {
        _wasBreached        = false;
        _lastBreachTime     = -BREACH_COOLDOWN;
        _lastCreatureTime   = -CREATURE_COOLDOWN;
        _wasOnFire          = false;
        _lastFireTime       = -FIRE_COOLDOWN;
        _oxyWasOffline      = false;
        _wasFloodingLow     = false;
        _wasFloodingCrit    = false;
        _musicPlaying       = false;
        _rolledForEncounter = false;
        _encounterActive    = false;
        _triggered          = false;
        _lastInRange        = false;
    }
}
#endif
