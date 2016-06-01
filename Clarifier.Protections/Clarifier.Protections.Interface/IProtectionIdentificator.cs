using Clarifier.Core;

namespace Clarifier.Identification.Interface
{
    public interface IProtectionIdentificator
    {
        bool Initialize(ClarifierContext ctx);
        double PerformIdentification(ClarifierContext ctx);
    }
}