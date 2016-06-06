using Clarifier.Core;
using System.Linq;

namespace Clarifier.Protection.Impl
{
    /// <summary>
    /// Anti-debug is a very simple protection.
    /// It is based on injecting a function inside the target assembly.
    /// This function is then called inside the constructor of the GlobalType.
    /// Removal of this protection is entirely based on pattern-matching.
    /// </summary>
    public class AntiDebug
    {
        BasicStaticProtection antiDebugSafe = new BasicStaticProtection();
        BasicStaticProtection antiDebugNet = new BasicStaticProtection();
        BasicStaticProtection antiDebugWin32 = new BasicStaticProtection();

        public AntiDebug()
        {
        }

        public bool Initialize(ClarifierContext ctx)
        {
            antiDebugSafe.AddPatternMatchingMethod("Confuser.Runtime.AntiDebugSafe", "Initialize");
            antiDebugSafe.AddPatternMatchingMethod("Confuser.Runtime.AntiDebugSafe", "Worker");

            antiDebugNet.AddPatternMatchingMethod("Confuser.Runtime.AntiDebugAntinet", "Initialize");

            antiDebugWin32.AddPatternMatchingMethod("Confuser.Runtime.AntiDebugWin32", "Initialize");
            antiDebugWin32.AddPatternMatchingMethod("Confuser.Runtime.AntiDebugWin32", "Worker");

            return antiDebugSafe.LoadTypes() && antiDebugNet.LoadTypes() && antiDebugWin32.LoadTypes();
        }

        public double PerformIdentification(ClarifierContext ctx)
        {
            double[] result = new double[3];
            result[0] = antiDebugSafe.MapSourceInDestination(ctx.CurrentModule);
            result[1] = antiDebugNet.MapSourceInDestination(ctx.CurrentModule);
            result[2] = antiDebugWin32.MapSourceInDestination(ctx.CurrentModule);
            return result.Max();
        }

        public bool PerformRemoval(ClarifierContext ctx)
        {
            return antiDebugSafe.PerformRemoval(ctx.CurrentModule) || 
                   antiDebugNet.PerformRemoval(ctx.CurrentModule)  || 
                   antiDebugWin32.PerformRemoval(ctx.CurrentModule);
        }
    }
}
