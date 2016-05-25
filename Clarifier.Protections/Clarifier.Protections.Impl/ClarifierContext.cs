using dnlib.DotNet;

namespace Clarifier.Identification.Impl
{
    public class ClarifierContext : IClarifierContext
    {
        public ModuleDef CurrentModule { get; set; }
    }
}