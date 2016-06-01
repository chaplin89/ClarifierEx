using Clarifier.Core;

namespace Clarifier.Protection.Impl
{
    /// <summary>
    /// Anti-dump is a very simple protection.
    /// It is based on injecting a function inside the target assembly.
    /// This function is then called inside the constructor of the GlobalType.
    /// Removal of this protection is entirely based on pattern-matching.
    /// </summary>
    public class AntiDumpIdentification
    {
        private BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();
        public AntiDumpIdentification()
        {
        }

        /// <summary>
        /// Add and load the blacklisted method
        /// </summary>
        /// <param name="ctx">Context</param>
        /// <returns></returns>
        public bool Initialize(ClarifierContext ctx)
        {
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.AntiDump", "Initialize");
            return staticProtectionsManager.LoadTypes();
        }
        /// <summary>
        /// Search in the module for similar methods.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public double PerformIdentification(ClarifierContext ctx)
        {
            return staticProtectionsManager.MapSourceInDestination(ctx.CurrentModule);            
        }

        /// <summary>
        /// Remove every reference and nullify the blacklisted method
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public bool PerformRemoval(ClarifierContext ctx)
        {
            return staticProtectionsManager.PerformRemoval(ctx.CurrentModule);
        }
    }
}
