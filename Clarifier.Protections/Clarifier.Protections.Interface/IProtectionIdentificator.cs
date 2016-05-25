
namespace Clarifier.Identification.Impl
{
    public interface IProtectionIdentificator
    {
        bool Initialize(IClarifierContext ctx);
        double PerformIdentification(IClarifierContext ctx);
    }
}