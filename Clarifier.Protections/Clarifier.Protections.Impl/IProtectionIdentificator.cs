using dnlib.DotNet;

namespace Clarifier.Identification.Impl
{
    internal interface IProtectionIdentificator
    {
        bool Initialize(ClarifierContext ctx);
        double PerformIdentification(ClarifierContext ctx);
    }
}