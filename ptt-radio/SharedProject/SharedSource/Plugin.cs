using Barotrauma.Sounds;
using Barotrauma.Networking;

namespace PTTRadio
{
    public class Plugin : IAssemblyPlugin
    {
#if CLIENT
        private Sound? sndHigh;
        private Sound? sndOff;
        private Sound? sndLow;

        private bool wasTransmittingRadio = false;
        private bool pendingTransmitRadio = false;
        private readonly Dictionary<int, bool> wasReceivingFromClient = new();
        private readonly Dictionary<int, int> silenceFramesFromClient = new();

        // Frames of silence required before a remote client is considered to have stopped
        // transmitting. At ~60fps this is ~0.5 s, long enough to bridge inter-packet gaps.
        private const int SilenceFrameThreshold = 30;
#endif

        public void Initialize()
        {
#if CLIENT
            string modDir = GetModDirectory();

            try
            {
                var sm = GameMain.SoundManager;
                sndHigh = sm?.LoadSound(Path.Combine(modDir, "Sounds", "PTTHigh.ogg"), false);
                sndOff  = sm?.LoadSound(Path.Combine(modDir, "Sounds", "PTTClickOff.ogg"), false);
                sndLow  = sm?.LoadSound(Path.Combine(modDir, "Sounds", "PTTLow.ogg"), false);
            }
            catch (Exception e)
            {
                LuaCsLogger.Log($"[PTTRadio] Failed to load sounds from '{modDir}': {e.Message}");
            }

#pragma warning disable CS0618
            LuaCsSetup.Instance.Hook.Add("think", "pttRadioThink", (object[] _) =>
            {
                Update();
                return null;
            });
#pragma warning restore CS0618

            LuaCsLogger.Log($"[PTTRadio] Loaded — {modDir}");
#endif
        }

#if CLIENT
        private void Update()
        {
            bool isTransmitting = IsLocalPlayerOnRadio();

            if (!isTransmitting)
            {
                if (wasTransmittingRadio) Play(sndOff);
                wasTransmittingRadio = false;
                pendingTransmitRadio = false;
            }
            else if (!wasTransmittingRadio)
            {
                if (pendingTransmitRadio)
                {
                    Play(sndHigh);
                    wasTransmittingRadio = true;
                    pendingTransmitRadio = false;
                }
                else
                {
                    pendingTransmitRadio = true;
                }
            }

            CheckIncomingVoice();
        }

        private static bool IsLocalPlayerOnRadio()
        {
            try
            {
                var character = Character.Controlled;
                if (character == null) return false;
                if (!PlayerInput.KeyDown(InputType.Voice)) return false;
                if (GUI.KeyboardDispatcher.Subscriber != null) return false;
                if (VoipCapture.Instance == null) return false;
                if (VoipCapture.Instance.ForceLocal) return false;
                return ChatMessage.CanUseRadio(character, out _);
            }
            catch { return false; }
        }

        private void CheckIncomingVoice()
        {
            try
            {
                var connectedClients = GameMain.Client?.ConnectedClients;
                if (connectedClients == null) return;

                var myChar = Character.Controlled;
                foreach (Client client in connectedClients)
                {
                    if (client.Character == myChar) continue;

                    int id = client.SessionId;

                    bool active = false;
                    try
                    {
                        active = client.VoipSound != null &&
                                 client.VoipSound.CurrentAmplitude > 0.01f &&
                                 client.VoipQueue != null &&
                                 !client.VoipQueue.ForceLocal &&
                                 client.Character != null &&
                                 ChatMessage.CanUseRadio(client.Character, out _);
                    }
                    catch { /* client may have disconnected mid-loop */ }

                    bool wasActive = wasReceivingFromClient.GetValueOrDefault(id, false);

                    if (active)
                    {
                        silenceFramesFromClient[id] = 0;
                        if (!wasActive)
                        {
                            Play(sndLow);
                            wasReceivingFromClient[id] = true;
                        }
                    }
                    else if (wasActive)
                    {
                        int silence = silenceFramesFromClient.GetValueOrDefault(id, 0) + 1;
                        silenceFramesFromClient[id] = silence;
                        if (silence >= SilenceFrameThreshold)
                            wasReceivingFromClient[id] = false;
                    }
                }
            }
            catch { /* ClientList may change during iteration on disconnect */ }
        }

        private static void Play(Sound? sound)
        {
            try { sound?.Play(1.0f); }
            catch { }
        }

        private static string GetModDirectory()
        {
            // DLL is at: <modRoot>/bin/Client/Windows/PTTRadio.dll
            // Walk up three levels to reach mod root.
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            for (int i = 0; i < 3; i++)
                path = Directory.GetParent(path)?.FullName ?? path;
            return path;
        }
#endif

        public void OnLoadCompleted() { }

        public void PreInitPatching() { }

        public void Dispose()
        {
#if CLIENT
#pragma warning disable CS0618
            try { LuaCsSetup.Instance.Hook.Remove("think", "pttRadioThink"); }
#pragma warning restore CS0618
            catch { }

            wasTransmittingRadio = false;
            pendingTransmitRadio = false;
            wasReceivingFromClient.Clear();
            silenceFramesFromClient.Clear();

            try { sndHigh?.Dispose(); } catch { }
            try { sndOff?.Dispose();  } catch { }
            try { sndLow?.Dispose();  } catch { }

            LuaCsLogger.Log("[PTTRadio] Disposed.");
#endif
        }
    }
}
