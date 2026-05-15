global using System;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using Barotrauma;
global using Barotrauma.LuaCs;
global using Barotrauma.Networking;
global using Microsoft.Xna.Framework;

// Grant access to internal members of all three Barotrauma assemblies.
// The Publicized DLLs already make everything public, but these attributes
// also suppress C# accessibility diagnostics and are required when linking
// against non-publicized DLLs.
[assembly: IgnoresAccessChecksTo("Barotrauma")]
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]
[assembly: IgnoresAccessChecksTo("DedicatedServer")]
