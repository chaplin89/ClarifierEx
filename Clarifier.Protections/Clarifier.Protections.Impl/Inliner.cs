using Clarifier.Core;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using FuzzyEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Clarifier.Protection.Impl
{
    /// <summary>
    /// This class manage the "reference proxy" protection.
    /// This protection is based on creating proxy function that should hide
    /// the real function.
    /// Approach here is based on replacing all the calls to these proxy methods
    /// with calls to the real method.
    /// </summary>
    public class Inliner
    {
        BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();

        Dictionary<MethodDef, List<InstructionGroup>> referenceProxyMethods = new Dictionary<MethodDef, List<InstructionGroup>>();

        public void PerformRemoval(ClarifierContext ctx)
        {
            foreach (var method in ctx.CurrentModule.GetMethods())
            {
                if (referenceProxyMethods.Keys.Contains(method))
                    continue;
                foreach (var instruction in method.GetInstructions())
                {
                    if (instruction.OpCode != OpCodes.Call)
                        continue;

                    MethodDef targetMethod = null;
                    if (instruction.Operand is MethodDef)
                    {
                        targetMethod = instruction.Operand as MethodDef;
                    }
                    else if (instruction.Operand is MethodSpec)
                    {
                        MethodSpec tempMethod = instruction.Operand as MethodSpec;
                        targetMethod = tempMethod.Method as MethodDef;
                    }
                    else if (instruction.Operand is MemberRef)
                    {
                        continue;
                    }

                    Debug.Assert(targetMethod != null);

                    List<InstructionGroup> currentInstructionGroup;
                    if (referenceProxyMethods.TryGetValue(targetMethod, out currentInstructionGroup))
                    {
                        int callIndex = currentInstructionGroup.Where(x => x.Name == "Call").First().FoundInstructions[0];
                        instruction.Operand = targetMethod.Body.Instructions[callIndex].Operand;
                    }
                }
            }
        }
        public double PerformIdentification(ClarifierContext ctx)
        {
            FuzzyNode loadStage = new FuzzyNode(ctx.ILLanguage["ArgumentLoad"].Childs)
            {
                Name = "LoadStage",
                MaxNumber = null,
                MinNumber = 1,
                Mode = TestMode.InRange
            };
            FuzzyNode callStage = new FuzzyNode(ctx.ILLanguage["Call"].Childs)
            {
                Name = "CallStage",
                MaxNumber = 1,
                MinNumber = 1,
                Mode = TestMode.InRange
            };
            FuzzyNode returnStage = new FuzzyNode(ctx.ILLanguage["Return"].Childs)
            {
                Name = "ReturnStage",
                MaxNumber = 1,
                MinNumber = 1,
                Mode = TestMode.InRange
            };

            FuzzyNode proxyCall = new FuzzyNode(new FuzzyNode[] { loadStage, callStage, returnStage })
            {
                Name = "ProxyCall",
                MinNumber = 1,
                MaxNumber = 1,
                Mode = TestMode.MatchEverything
            };

            foreach (var method in ctx.CurrentModule.GetMethods())
            { 
                if (!method.HasBody)
                {
                    continue;
                }

                ComparisonContext compCtx = new ComparisonContext()
                {
                    InstructionList = method.Body.Instructions.ToList()
                };

                if (proxyCall.Test(compCtx) == ConditionOutcome.Matched)
                {
                    referenceProxyMethods[method] = null;
                }
            }

            if (referenceProxyMethods.Count != 0)
                return 1.0;
            return 0.0;
        }

        public void Initialize()
        {
        }
    }
}
