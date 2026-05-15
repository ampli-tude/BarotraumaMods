namespace CyclopsVoices;

internal static class NetBridge
{
    private const string MSG = "AbandonShip_Event";
    private static INetworkingService? _net;

    public static void Initialize()
    {
        try
        {
#pragma warning disable CS0618
            _net = LuaCsSetup.Instance.NetworkingService;
#pragma warning restore CS0618
        }
        catch (Exception e)
        {
            LuaCsLogger.Log($"[CyclopsVoices] Failed to get NetworkingService: {e.Message}");
        }

        if (_net == null)
        {
            LuaCsLogger.Log("[CyclopsVoices] NetworkingService unavailable — multiplayer sync disabled.");
            return;
        }

#if CLIENT
        try
        {
            _net.Receive(MSG, (IReadMessage netMsg) =>
            {
                try
                {
                    string eventType = netMsg.ReadString();
                    string action    = netMsg.ReadString();
                    AudioManager.OnEvent(eventType, action);
                }
                catch (Exception e)
                {
                    LuaCsLogger.Log($"[CyclopsVoices] Receive dispatch error: {e.Message}");
                }
            });
        }
        catch (Exception e)
        {
            LuaCsLogger.Log($"[CyclopsVoices] Receive setup error: {e.Message}");
        }
#endif
    }

    // Broadcast to all clients (connection = null) or to a specific connection.
    public static void Send(string eventType, string action = "play")
    {
#if SERVER
        SendToConnection(eventType, action, null);
#endif
    }

#if SERVER
    public static void SendToConnection(string eventType, string action, NetworkConnection? conn)
    {
        if (_net == null) return;
        try
        {
            var msg = _net.Start(MSG);
            msg.WriteString(eventType);
            msg.WriteString(action);
            _net.SendToClient(msg, conn);
        }
        catch (Exception e)
        {
            LuaCsLogger.Log($"[CyclopsVoices] Send error: {e.Message}");
        }
    }
#endif

    public static void Dispose() => _net = null;
}
