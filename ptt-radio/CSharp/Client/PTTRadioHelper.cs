using Barotrauma;

// C# helper for ptt_radio.lua.
// LuaCs whitelists block VoipCapture and client.VoipQueue from Lua,
// so this file reads them from C# (InternalsAwareAssembly context) and
// exposes the results as a public static class that Lua can call directly.

public static class PTTRadioHelper
{
    /// <summary>
    /// Returns true when the local player has Radio mode selected AND has
    /// functioning radio hardware — mirrors the Soundproof-Walls isRadio check:
    ///   !VoipCapture.Instance.ForceLocal && ChatMessage.CanUseRadio(char)
    /// ForceLocal is true when "Local" is selected, false when "Radio" is selected.
    /// </summary>
    public static bool IsInRadioMode()
    {
        try
        {
            var character = Character.Controlled;
            if (character == null) return false;

            // VoipCapture.Instance is null when PTT is not held.
            // Defaulting ForceLocal to true (= local mode) when null is correct:
            // if capture hasn't started we are not transmitting on radio.
            bool forceLocal = VoipCapture.Instance?.ForceLocal ?? true;
            if (forceLocal) return false;

            return ChatMessage.CanUseRadio(character, out _);
        }
        catch { return false; }
    }

    /// <summary>
    /// Returns true when a remote client is transmitting in Radio mode.
    /// Reads client.VoipQueue.ForceLocal, which is inaccessible from Lua.
    /// </summary>
    public static bool IsClientOnRadio(Client client)
    {
        try
        {
            if (client?.VoipQueue == null || client.Character == null) return false;
            if (client.VoipQueue.ForceLocal) return false;
            return ChatMessage.CanUseRadio(client.Character, out _);
        }
        catch { return false; }
    }
}
