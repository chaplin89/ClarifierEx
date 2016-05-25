using Clarifier.Core;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Clarifier.Identification.Impl
{
    public class Constants : BasicStaticProtection
    {
        public Constants()
        {
            blacklist = new List<Tuple<string, string>>
            {
                Tuple.Create("Confuser.Runtime.Constant","Get"),
                Tuple.Create("Confuser.Runtime.Constant","Initialize")
            };
        }

        public override bool Initialize(IClarifierContext ctx)
        {
            return base.Initialize(ctx);
        }

        public override bool PerformRemoval(IClarifierContext ctx)
        {
            byte[] newAssembly = ClarifierInjectHelper.GetBrandNewAssemblyFromType(ctx.CurrentModule.GlobalType);

            if (Debugger.IsAttached)
            {
                File.WriteAllBytes(@".\TestAssembly.dll", newAssembly);
            }

            Assembly asm = Assembly.Load(newAssembly);

            Type dummyType = asm.ManifestModule.GetType("DummyNamespace.DummyType");
            object dummyInstance = Activator.CreateInstance(dummyType);

            Dictionary<string, MethodInfo> mapNewMethodsToName = new Dictionary<string, MethodInfo>();

            foreach(var v in blacklistMapInDestination)
            {
                foreach(var vv in v.Value)
                {
                    string currentName = string.Format("DummyNamespace.DummyType.{0}", vv.FullName);
                    mapNewMethodsToName[currentName] = dummyType.GetMethod(vv.Name);
                }
            }

            // Map string -> Destination method(s)
            foreach (var identifiedMethods in blacklistMapInDestination)
            {
                // Foreach methods in destination
                foreach (var currentIdentifiedMethod in identifiedMethods.Value)
                {
                    // Foreach type in destination assembly
                    foreach (var currentType in AllTypesHelper.Types(ctx.CurrentModule.Types))
                    {
                        // Foreach method in destination type
                        foreach (var currentMethod in currentType.Methods)
                        {
                            if (currentMethod == currentIdentifiedMethod)
                                continue;

                            foreach(var currentInstruction in currentMethod.GetInstruction())
                            {
                                
                                if (currentInstruction.OpCode == OpCodes.Call && ((IMethod)currentInstruction.Operand).Name == currentIdentifiedMethod.Name)
                                {
                                    //Call this method
                                    mapNewMethodsToName[currentIdentifiedMethod.Name].Invoke(null, new object[] { });
                                }
                            }

                        }
                    }
                }
            }

            foreach (var v in mapNewMethodsToName)
            {
                if (v.Value.IsGenericMethod)
                {
                }

                try
                {
                    object wtfff = mapNewMethodsToName[v.Key].MakeGenericMethod(typeof(string)).Invoke(null, new object[] { 226098525u });
                }
                catch
                {
                }
            }
            return true;
//             BodyModifier.FindAndReplaceWithResult(toReplace, targetModule, mapMethodsToName, dummyInstance);
//             foreach (var v in identifiedMethods)
//             {
//                 foreach (var currentType in AllTypesHelper.Types(ctx.CurrentModule.Types))
//                 {
//                     foreach (var currentMethod in currentType.Methods)
//                     {
//                         if (v != currentMethod)
//                         {
// 
//                         }
//                     }
//                 }
//             }
//             return true;
        }

        public override double PerformIdentification(IClarifierContext ctx)
        {
            return base.PerformIdentification(ctx);
        }
    }
}
