// IgnoresAccessChecksToAttribute is not in the standard .NET SDK.
// Defining it here allows the [assembly: IgnoresAccessChecksTo(...)] attributes
// in AssemblySettings.cs to compile, which suppresses C# accessibility diagnostics
// when referencing internal Barotrauma types.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName) { }
    }
}
