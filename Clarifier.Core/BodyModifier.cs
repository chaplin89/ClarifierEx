using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Clarifier.Core
{
    static public class BodyModifier
    {
        static public bool RemoveReferences(List<MethodDef> blacklist, ModuleDef targetModule)
        {
            bool returnValue = true;
            foreach (var v in blacklist)
            {
                foreach (var currentMethodToRemove in BodyComparison.GetSimilarMethods(targetModule, v, true, 0.70))
                {
                    foreach (var currentType in AllTypesHelper.Types(targetModule.Types))
                    {
                        foreach (var currentMethod in currentType.Methods)
                        {
                            if (currentMethod != currentMethodToRemove)
                            {
                                returnValue |= RemoveRefence(currentMethod, currentMethodToRemove);
                            }
                        }

                    }

                    NullifyMethod(currentMethodToRemove);
                }
            }
            return returnValue;
        }

        static public void FindAndReplaceWithResult(List<MethodDef> toFind, ModuleDefMD targetModule)
        {
            foreach (var v in toFind)
            {
                foreach (var toReplace in BodyComparison.GetSimilarMethods(targetModule, v, true, 0.70))
                {
                    foreach (var currentType in AllTypesHelper.Types(targetModule.Types))
                    {
                        foreach (var currentMethod in currentType.Methods)
                        {
                            if (!currentMethod.HasBody)
                                continue;

                            for (var i = 0; i < currentMethod.Body.Instructions.Count; ++i)
                            {
                                if (currentMethod.Body.Instructions[i].Operand == null)
                                    continue;

                                if (currentMethod.Body.Instructions[i].Operand is MethodDef && currentMethod.Body.Instructions[i].Operand == toReplace)
                                {
                                    if (toReplace.Body.Instructions[i].OpCode == OpCodes.Call)
                                    { 
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static public bool RemoveRefence(MethodDef methodToInspect, MethodDef methodToLookFor)
        {
            bool cantPurge = false;

            if (methodToInspect.Body == null)
                return true;

            for (var i = 0; i < methodToInspect.Body.Instructions.Count; ++i)
            {
                if (methodToInspect.Body.Instructions[i].Operand is MethodDef && methodToInspect.Body.Instructions[i].Operand == methodToLookFor)
                {
                    if (methodToInspect.Body.Instructions[i].OpCode == OpCodes.Call)
                    {
                        int j = i;
                        for (; j >= i - methodToLookFor.Parameters.Count; j--)
                        {
                            methodToInspect.Body.Instructions.RemoveAt(j);
                        }
                        i = j;
                        continue;
                    }
                    else
                    {
                        cantPurge = true;
                    }
                }
            }

            return cantPurge;
        }

        static public void NullifyMethod(MethodDef method)
        {
            if (method.ReturnType.FullName == "System.Void")
            {
                method.Body.Instructions.Clear();
                method.Body.Instructions.Add(new Instruction(OpCodes.Ret));
            }
            else if (method.ReturnType.IsValueType)
            {

            }
            else if (method.ReturnType.IsByRef)
            {

            }
        }
    }
}
