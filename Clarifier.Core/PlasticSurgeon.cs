using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.IO;

namespace Clarifier.Core
{
    public class PlasticSurgeon
    {
        public bool RemoveReferences(ModuleDef blacklistModule, List<KeyValuePair<string, string>> blacklist, ModuleDef targetModule)
        {
            bool returnValue = true;
            foreach (var v in blacklist)
            {
                MethodDef blacklistMethod = blacklistModule.Find(v.Key, true).FindMethod(v.Value);

                foreach (var currentMethodToRemove in BodyComparison.GetSimilarMethods(targetModule, blacklistMethod, true, 0.70))
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

        public void ReplaceWithResult(ModuleDefMD confuserRuntimeModule, List<KeyValuePair<string, string>> toReplace, ModuleDefMD targetModule)
        {
            bool returnValue = true;
            foreach (var v in toReplace)
            {
                MethodDef blacklistMethod = confuserRuntimeModule.Find(v.Key, true).FindMethod(v.Value);

                foreach (var currentMethodToReplace in BodyComparison.GetSimilarMethods(targetModule, blacklistMethod, true, 0.70))
                {
                    Assembly axx = Assembly.Load(targetModule.Assembly.FullName);
                    Type[] wtf = axx.GetTypes();

                    foreach (var currentType in AllTypesHelper.Types(targetModule.Types))
                    {
                        foreach (var currentMethod in currentType.Methods)
                        {
                            if (!currentMethod.HasBody)
                                continue;

                            for (var i = 0; i < currentMethod.Body.Instructions.Count; ++i)
                            {
                                if (currentMethod.Body.Instructions[i].Operand is MethodDef && currentMethod.Body.Instructions[i].Operand == currentMethodToReplace)
                                {
                                    if (currentMethodToReplace.Body.Instructions[i].OpCode == OpCodes.Call)
                                    {
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool RemoveRefence(MethodDef methodToInspect, MethodDef methodToLookFor)
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

        public void NullifyMethod(MethodDef method)
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
