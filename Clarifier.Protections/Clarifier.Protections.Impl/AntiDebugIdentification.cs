using Clarifier.Core;

namespace Clarifier.Protection.Impl
{
    /// <summary>
    /// Anti-debug is a very simple protection.
    /// It is based on injecting a function inside the target assembly.
    /// This function is then called inside the constructor of the GlobalType.
    /// Removal of this protection is entirely based on pattern-matching.
    /// </summary>
    public class AntiDebugIdentification
    {
        BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();

        public AntiDebugIdentification()
        {
        }

        public bool Initialize(ClarifierContext ctx)
        {
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.AntiDebugSafe", "Initialize");
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.AntiDebugSafe", "Worker");

            return staticProtectionsManager.LoadTypes();
        }

        public double PerformIdentification(ClarifierContext ctx)
        {
            return staticProtectionsManager.MapSourceInDestination(ctx.CurrentModule);
        }

        public bool PerformRemoval(ClarifierContext ctx)
        {
            return staticProtectionsManager.PerformRemoval(ctx.CurrentModule);
        }
    }
}
