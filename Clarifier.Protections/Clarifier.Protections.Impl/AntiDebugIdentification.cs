using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using Clarifier.Core;
using System.IO;

namespace Clarifier.Identification.Impl
{
    public class AntiDebugIdentification
    {
        BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();

        public AntiDebugIdentification()
        {
        }

        public bool Initialize(IClarifierContext ctx)
        {
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.AntiDebugSafe", "Initialize");
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.AntiDebugSafe", "Worker");

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
