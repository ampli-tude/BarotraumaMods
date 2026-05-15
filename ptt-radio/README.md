# PTT Radio Tones

A client-side [Barotrauma](https://store.steampowered.com/app/602960/Barotrauma/) mod that plays audio cues on voice transmission events, similar to real radio communication.

## What it does

| Event | Sound | Who hears it |
|---|---|---|
| You press PTT while in **Radio** mode | `PTTHigh.ogg` | You only |
| You release PTT | `PTTClickOff.ogg` | You only |
| A remote player starts transmitting on **Radio** | `PTTLow.ogg` | You only |

Sounds only fire in **Radio** mode. Switching to Local mode suppresses all cues. The mode is the Local/Radio toggle visible in the chat box (press **R** or use the dropdown).

## Requirements

- [LuaForBarotrauma](https://steamcommunity.com/sharedfiles/filedetails/?id=2559634234) — required for the C# assembly plugin system this mod uses

## Installation

### From Steam Workshop
*Publish Pending*

### Manual install

1. Download or clone this repository
2. Copy the folder into your Barotrauma LocalMods directory:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Barotrauma\LocalMods\PTT Radio Tones\
   ```
3. Launch Barotrauma → **Main Menu → Mods** (jigsaw icon) → enable **PTT Radio Tones** → apply

**Confirm it loaded** by opening the debug console (F3 in-game) and looking for:
```
[PTTRadio] Loaded — <path>
```

## Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build -c Release
```

The compiled DLL is placed at `bin\Client\Windows\PTTRadio.dll` automatically.

For a faster dev loop, create a junction link so the mod folder is served directly from the repo:

```cmd
mklink /J "C:\Program Files (x86)\Steam\steamapps\common\Barotrauma\LocalMods\PTT Radio Tones" "F:\workspace\ptt-radio"
```

After that, rebuilding is enough — no manual file copying needed.

### Reference DLLs

The `Ref\` folder must be populated before the project will compile. It is not included in the repository.

1. Download the latest `luacsforbarotrauma_refs.zip` from the [LuaCsForBarotrauma Releases page](https://github.com/evilfactory/LuaCsForBarotrauma/releases/download/latest/luacsforbarotrauma_refs.zip).

2. Extract the ZIP contents into the `Ref\` folder. The result should look like:
   ```
   ptt-radio/
   └── Ref/
       ├── Windows/
       │   ├── Barotrauma.dll
       │   └── BarotraumaCore.dll
       ├── MonoGame.Framework.Windows.NetStandard.dll
       └── ... (other shared refs)
   ```

3. Copy `NVorbis.dll` from your Barotrauma install directory into `Ref\`.

If Barotrauma updates and the DLLs go out of date, repeat these steps and rebuild.

## Notes

- **Windows only** — only a Windows client build is included. Linux/macOS support would require adding the corresponding project files and platform DLLs.
- **No server component** — the mod is purely client-side. It does not need to be installed on the server or by other players.
- **Sound files must be OGG** — to replace the included sounds, convert with: `ffmpeg -i input.wav -c:a libvorbis -q:a 4 output.ogg`

## License

MIT
