using dnlib.DotNet;

namespace Clarifier.Identification.Impl
{
    public interface IClarifierContext
    {
        ModuleDef CurrentModule { get; set; }
    }
}