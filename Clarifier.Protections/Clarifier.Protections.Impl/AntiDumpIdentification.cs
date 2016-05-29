
namespace Clarifier.Identification.Impl
{
    public class AntiDumpIdentification
    {
        private BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();
        public AntiDumpIdentification()
        {
        }

        public bool Initialize(IClarifierContext ctx)
        {
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.AntiDump", "Initialize");
            return staticProtectionsManager.LoadTypes();
        }

        public double PerformIdentification(IClarifierContext ctx)
        {
            return staticProtectionsManager.MapSourceInDestination(ctx.CurrentModule);            
        }

        public bool PerformRemoval(IClarifierContext ctx)
        {
            return staticProtectionsManager.PerformRemoval(ctx.CurrentModule);
        }
    }
}
