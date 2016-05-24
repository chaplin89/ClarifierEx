using dnlib.DotNet;

namespace Clarifier.Identification.Impl
{
    internal class ClarifierContext
    {
        public ModuleDef CurrentModule { get; internal set; }
    }
}