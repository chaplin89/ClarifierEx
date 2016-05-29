using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarifier.Identification.Impl
{
    public class AntiTamper
    {
        BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();

        public void PerformRemoval(IClarifierContext ctx)
        {
            foreach(var v in staticProtectionsManager.DestinationMap)
            {
                foreach (var vv in v.matchingMethods)
                {
                    Debugger.Break();
                    var typeRef = ctx.CurrentModule.UpdateRowId(new TypeRefUser(ctx.CurrentModule, "System.Diagnostics", "Debugger", ctx.CurrentModule.CorLibTypes.AssemblyRef));
                    var classSignature = new ClassSig(typeRef);

                    var methodSig = MethodSig.CreateStatic(ctx.CurrentModule.CorLibTypes.Void);
                    MemberRefUser mff = ctx.CurrentModule.UpdateRowId(new MemberRefUser(ctx.CurrentModule, "Break", methodSig, classSignature.TypeDefOrRef));

                    vv.Body.Instructions.Insert(vv.Body.Instructions.Count-1,Instruction.Create(OpCodes.Call, mff));
                }
            }
        }
        public double PerformIdentification(IClarifierContext ctx)
        {
            return staticProtectionsManager.MapSourceInDestination(ctx.CurrentModule);
        }

        public  void Initialize()
        {
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.AntiTamperNormal", "Initialize"); ;
            staticProtectionsManager.LoadTypes();
        }
    }
}
