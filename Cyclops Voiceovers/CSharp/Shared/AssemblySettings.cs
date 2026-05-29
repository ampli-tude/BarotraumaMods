global using System;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using Barotrauma;
global using Barotrauma.LuaCs;
global using Barotrauma.Networking;
global using Microsoft.Xna.Framework;

// Grant runtime access to internal members of Barotrauma assemblies.
// IgnoresAccessChecksToAttribute is built into .NET 8 — no custom definition needed.
[assembly: IgnoresAccessChecksTo("Barotrauma")]
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]
[assembly: IgnoresAccessChecksTo("DedicatedServer")]

