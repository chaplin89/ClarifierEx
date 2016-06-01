using Clarifier.Core;

namespace Clarifier.Identification.Interface
{
    public interface IProtectionRemover
    {
        bool PerformRemoval(ClarifierContext ctx);
    }
}