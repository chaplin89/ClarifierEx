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
    public class AntiDebugIdentification : BasicStaticProtection
    {
        public AntiDebugIdentification()
        {
            blacklist = new List<Tuple<string, string>>
            {
                Tuple.Create("Confuser.Runtime.AntiDebugSafe","Initialize"),
                Tuple.Create("Confuser.Runtime.AntiDebugSafe","Worker"),
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
