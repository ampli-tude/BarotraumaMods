# Cyclops Voiceovers

A [Barotrauma](https://store.steampowered.com/app/602960/Barotrauma/) mod that adds submarine AI voice lines for in-game events — hull breaches, flooding, fires, creature attacks, and more.

## What it does

| Event | Voice line | Notes |
|---|---|---|
| New exterior hull breach | `hull_breach.ogg` or `AI_external_damage.ogg` (random) | 15 sec cooldown |
| Creature near a breach | `creature_attack.ogg` | 30 sec cooldown, plays before breach line |
| All hull breaches sealed | `leak_fix.ogg` | — |
| Fire detected | `fire_detected.ogg` | 20 sec cooldown |
| All fires extinguished | `AI_fire_extinguished.ogg` | — |
| Average hull oxygen drops below 95% | `oxy_off.ogg` | — |
| Flooding ≥ 15% of hull volume | `AI_hull_low.ogg` | 40 sec cooldown |
| Flooding ≥ 30% of hull volume | `AI_hull_crit.ogg` | 40 sec cooldown |
| Boss creature or enemy sub within 10,000 units | `Abandon Ship.ogg` (looping music) | 20% chance; fades in/out; resumes if re-entering range |

Voice lines are queued and play one at a time. Encounter music plays independently on a separate channel.

## Requirements

- [LuaForBarotrauma](https://steamcommunity.com/sharedfiles/filedetails/?id=2559634234) — required for the C# assembly plugin system this mod uses

## Installation

### From Steam Workshop
*Publish Pending*

### Manual install

1. Download or clone this repository
2. Copy the folder into your Barotrauma LocalMods directory:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Barotrauma\LocalMods\Cyclops Voiceovers\
   ```
3. Launch Barotrauma → **Main Menu → Mods** (jigsaw icon) → enable **Cyclops Voiceovers** → apply

## Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
.\build.ps1
```

The script builds both the Client and Server DLLs and syncs them to LocalMods automatically.

For a faster dev loop, create a junction link so the mod folder is served directly from the repo:

```cmd
mklink /J "C:\Program Files (x86)\Steam\steamapps\common\Barotrauma\LocalMods\Cyclops Voiceovers" "F:\workspace\BarotraumaMods\Cyclops Voiceovers"
```

After that, running `.\build.ps1` is enough — no manual file copying needed.

### Reference DLLs

The `CSharp\Ref\` folder must be populated before the project will compile. It is not included in the repository.

1. Download the latest `luacsforbarotrauma_refs.zip` from the [LuaCsForBarotrauma Releases page](https://github.com/evilfactory/LuaCsForBarotrauma/releases/download/latest/luacsforbarotrauma_refs.zip).

2. Extract the ZIP contents into the `Ref\` folder. The result should look like:
   ```
   Cyclops Voiceovers/
   └── Ref/
       ├── Windows/
       │   ├── Barotrauma.dll
       │   └── BarotraumaCore.dll
       ├── MonoGame.Framework.Windows.NetStandard.dll
       └── ... (other shared refs)
   ```

If Barotrauma updates and the DLLs go out of date, repeat these steps and rebuild.

## Notes

- **Windows only** — only a Windows client build is included.
- **Both client and server DLLs required** — the server detects events and sends network messages; the client receives them and plays audio. Install the mod on the server (or host) and all clients.
- **Sound files must be OGG** — to replace or add sounds, convert with: `ffmpeg -i input.wav -c:a libvorbis -q:a 4 output.ogg`
- **Many voice lines are available but unused** — see `audio/CyclopsVoices/` for the full set. New triggers can be wired up in `CSharp/Shared/Detection.cs` and `AudioManager.cs`.

## License

MIT
