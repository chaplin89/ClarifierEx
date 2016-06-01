using Clarifier.Core;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Clarifier.Protection.Impl
{
    public class AntiTamper
    {
        BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();

        public void PerformRemoval(ClarifierContext ctx)
        {
            foreach(var v in staticProtectionsManager.DestinationMap)
            {
                foreach (var vv in v.matchingMethods)
                {
                    var typeRef = ctx.CurrentModule.UpdateRowId(new TypeRefUser(ctx.CurrentModule, "System.Diagnostics", "Debugger", ctx.CurrentModule.CorLibTypes.AssemblyRef));
                    var classSignature = new ClassSig(typeRef);

                    var methodSig = MethodSig.CreateStatic(ctx.CurrentModule.CorLibTypes.Void);
                    MemberRefUser mff = ctx.CurrentModule.UpdateRowId(new MemberRefUser(ctx.CurrentModule, "Break", methodSig, classSignature.TypeDefOrRef));

                    vv.Body.Instructions.Insert(vv.Body.Instructions.Count-1,Instruction.Create(OpCodes.Call, mff));
                }
            }
        }
        public double PerformIdentification(ClarifierContext ctx)
        {
            return staticProtectionsManager.MapSourceInDestination(ctx.CurrentModule);
        }

        public  void Initialize()
        {
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.AntiTamperNormal", "Initialize");
            staticProtectionsManager.LoadTypes();
        }
    }
}
