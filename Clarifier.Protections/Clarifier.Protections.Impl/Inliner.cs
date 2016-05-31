using Clarifier.Core;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarifier.Identification.Impl
{
    public class Inliner
    {
        BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();
        List<OpCode> loadInstructions = new List<OpCode>
        {
            OpCodes.Ldarg,
            OpCodes.Ldarga,
            OpCodes.Ldarga_S,
            OpCodes.Ldarg_0,
            OpCodes.Ldarg_1,
            OpCodes.Ldarg_2,
            OpCodes.Ldarg_3,
            OpCodes.Ldarg_S,
        };

        List<OpCode> callInstructions = new List<OpCode>
        {
                        OpCodes.Call,
            OpCodes.Calli,
            OpCodes.Callvirt,
        };

        List<OpCode> returnInstruction = new List<OpCode>
        {
            OpCodes.Ret
        };

        List<MethodDef> referenceProxyMethods = new List<MethodDef>();

        public void PerformRemoval(IClarifierContext ctx)
        {
            foreach (var method in ctx.CurrentModule.GetMethods())
            {
                foreach(var instruction in method.GetInstruction())
                {
                    MethodDef targetMethod = instruction.Operand as MethodDef;
                    if (instruction.OpCode == OpCodes.Call && referenceProxyMethods.Contains(targetMethod))
                    {
                        instruction.Operand = targetMethod.Body.Instructions[targetMethod.Body.Instructions.Count - 2].Operand ;
                    }
                }
            }
//             foreach(var types in AllTypesHelper.Types(ctx.CurrentModule.Types))
//             {
//                 foreach (var blacklist in referenceProxyMethods)
//                     types.Methods.Remove(blacklist);
//             }
        }
        public double PerformIdentification(IClarifierContext ctx)
        {
            foreach(var types in AllTypesHelper.Types(ctx.CurrentModule.Types))
            {
                foreach(var method in types.Methods)
                {
                    if (!method.HasBody)
                        continue;
                    bool isProxyMethod = true;

                    bool containLoad = false;
                    bool containCall = false;
                    bool containReturn = false;

                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (loadInstructions.Contains(instruction.OpCode))
                        {
                            containLoad = true;
                            continue;
                        }
                        else if (callInstructions.Contains(instruction.OpCode))
                        {
                            containCall = true;
                            continue;
                        }
                        else if (returnInstruction.Contains(instruction.OpCode))
                        {
                            containReturn = true;
                            continue;
                        }

                        isProxyMethod = false;
                        break;
                    }
                    isProxyMethod = isProxyMethod && containLoad && containCall && containReturn;
                    if (isProxyMethod)
                    {
                        referenceProxyMethods.Add(method);
                    }
                }
            }
            if (referenceProxyMethods.Count != 0)
                return 1.0;
            return 0.0;
        }

        public void Initialize()
        {
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.AntiTamperNormal", "Initialize");
            staticProtectionsManager.LoadTypes();
        }
    }
}
