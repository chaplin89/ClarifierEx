using Clarifier.Core;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clarifier.Identification.Impl
{
    class AntiDumpIdentification : BasicStaticProtection
    {
        public AntiDumpIdentification()
        {
            blacklist = new List<Tuple<string, string>>
            {
                Tuple.Create("Confuser.Runtime.AntiDump","Initialize"),
            };
        }

        public override bool Initialize(IClarifierContext ctx)
        {
            return base.Initialize(ctx);
        }

        public override double PerformIdentification(IClarifierContext ctx)
        {
            return base.PerformIdentification(ctx);            
        }

        public override bool PerformRemoval(IClarifierContext ctx)
        {
            return base.PerformRemoval(ctx);
        }
    }
}
