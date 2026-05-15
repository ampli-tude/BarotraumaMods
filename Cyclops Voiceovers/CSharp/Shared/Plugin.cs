namespace CyclopsVoices;

public class Plugin : IAssemblyPlugin
{
    internal static bool DebugMode = false;

    internal static void Log(string msg)
    {
        if (DebugMode) LuaCsLogger.Log(msg);
    }

#if SERVER
    private int _tickCounter = 0;
    private const int CHECK_TICKS = 60;
#endif

    public void Initialize()
    {
        string modDir = GetModDirectory();

#if CLIENT
        AudioManager.Initialize(modDir);
#endif
        NetBridge.Initialize();

#pragma warning disable CS0618
        LuaCsSetup.Instance.Hook.Add("think", "cyclopsVoicesThink", (object[] _) =>
        {
            OnThink();
            return null;
        });

        LuaCsSetup.Instance.Hook.Add("stop", "cyclopsVoicesStop", (object[] _) =>
        {
            OnStop();
            return null;
        });

#if SERVER
        LuaCsSetup.Instance.Hook.Add("playerConnected", "cyclopsVoicesPlayerConnected", (object[] args) =>
        {
            if (args.Length > 0 && args[0] is Client client)
                Detection.OnPlayerConnected(client);
            return null;
        });
#endif
#pragma warning restore CS0618

        LuaCsLogger.Log("[CyclopsVoices] Cyclops Voiceovers Loaded");
    }

    private void OnThink()
    {
#if CLIENT
        AudioManager.Tick();
#endif

#if SERVER
        _tickCounter++;
        if (_tickCounter < CHECK_TICKS) return;
        _tickCounter = 0;

        if (GameMain.GameSession == null) return;

        var mainSub = Submarine.MainSub;
        if (mainSub == null)
        {
            if (Detection.EncounterMusicPlaying)
                NetBridge.Send("encounter_music", "music_stop");
            return;
        }

        Detection.Check(mainSub);
#endif
    }

    private static void OnStop()
    {
#if SERVER
        if (Detection.EncounterMusicPlaying)
            NetBridge.Send("encounter_music", "music_stop");
        Detection.Reset();
#endif
#if CLIENT
        AudioManager.Reset();
#endif
    }

    // DLL location: <root>/bin/Client/Windows/AbandonShip.dll  (3 levels up)
    //           or: <root>/bin/Server/AbandonShip.dll           (2 levels up)
    private static string GetModDirectory()
    {
        string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
#if CLIENT
        for (int i = 0; i < 3; i++)
            path = System.IO.Directory.GetParent(path)?.FullName ?? path;
#else
        for (int i = 0; i < 2; i++)
            path = System.IO.Directory.GetParent(path)?.FullName ?? path;
#endif
        return path;
    }

    public void OnLoadCompleted() { }
    public void PreInitPatching() { }

    public void Dispose()
    {
#pragma warning disable CS0618
        try { LuaCsSetup.Instance.Hook.Remove("think", "cyclopsVoicesThink"); } catch { }
        try { LuaCsSetup.Instance.Hook.Remove("stop", "cyclopsVoicesStop"); } catch { }
#if SERVER
        try { LuaCsSetup.Instance.Hook.Remove("playerConnected", "cyclopsVoicesPlayerConnected"); } catch { }
#endif
#pragma warning restore CS0618

#if CLIENT
        AudioManager.Dispose();
#endif
        NetBridge.Dispose();
        LuaCsLogger.Log("[CyclopsVoices] Disposed.");
    }
}
