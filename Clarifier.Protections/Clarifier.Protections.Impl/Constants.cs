using Clarifier.Core;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

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
            List<MethodDef> onlyMethodsToSubstitute = blacklistMapInDestination.Where(x => x.Key.Item2 == "Get").First().Value;

            foreach (var v in blacklistMapInDestination)
            {
                foreach (var vv in v.Value)
                {
                    mapNewMethodsToName[vv.Name] = dummyType.GetMethod(vv.Name);
                }
            }

            // Foreach type in destination assembly
            foreach (var currentType in AllTypesHelper.Types(ctx.CurrentModule.Types))
            {
                // Foreach method in destination type
                foreach (var currentMethod in currentType.Methods)
                {
                    if (onlyMethodsToSubstitute.Exists(x => x == currentMethod))
                        continue;
                    if (!currentMethod.HasBody)
                        continue;

                    for(var i=0; i< currentMethod.Body.Instructions.Count; ++i)
                    {
                        Instruction currentInstruction = currentMethod.Body.Instructions[i];

                        if (currentInstruction.OpCode != OpCodes.Call)
                            continue;

                        IMethod targetMethod = (IMethod)currentInstruction.Operand;
                        MethodInfo methodToInvoke;

                        if (!onlyMethodsToSubstitute.Exists(x => x.Name == targetMethod.Name))
                            continue;

                        if (mapNewMethodsToName.TryGetValue(targetMethod.Name, out methodToInvoke))
                        {
                            int id = (int)currentMethod.Body.Instructions[i - 1].Operand;
                            int inputParameters = methodToInvoke.GetParameters().Count();
                            object[] parameters = new object[inputParameters];
                            int j = i;

                            if (methodToInvoke.IsGenericMethod)
                            {
                                MethodSpec genericMethod = (MethodSpec)targetMethod;
                                Type[] genericTypes = genericMethod.GenericInstMethodSig.GenericArguments.Select(x => Type.GetType(x.ReflectionFullName)).ToArray();
                                methodToInvoke = methodToInvoke.MakeGenericMethod(genericTypes);
                            }

                            for (; j > i - inputParameters; j--)
                            {
                                Type targetType = methodToInvoke.GetParameters()[parameters.Length - (i - j)-1].ParameterType;
                                object operand = currentMethod.Body.Instructions[j - 1].Operand;

                                if (targetType.IsValueType)
                                {
                                    try
                                    {
                                        parameters[parameters.Length - (i - j) - 1] = (uint)(int)operand;
                                    }
                                    catch
                                    {
                                        parameters[parameters.Length - (i - j) - 1] = Convert.ChangeType(operand, targetType);
                                    }
                                }
                                else
                                {
                                    parameters[parameters.Length - (i - j)-1] = operand;
                                }
                                MethodSpec genericMethod = (MethodSpec)targetMethod;

                                if (methodToInvoke.ReturnType == typeof(string))
                                    currentMethod.Body.Instructions.RemoveAt(j - 1);
                                else
                                {
                                   // Debugger.Break();

                                }
                            }
                            if (methodToInvoke.ReturnType == typeof(string))
                                i = j;

                            //methodToInvoke.ReturnType;
                            //object returnedObject = Activator.CreateInstance();
                            object returnedObject = methodToInvoke.Invoke(null, parameters);

                            if (returnedObject.GetType() == typeof(string))
                            {
                                currentMethod.Body.Instructions[i] = new Instruction(OpCodes.Ldstr, returnedObject);
                            }
                            else if (returnedObject.GetType().IsArray)
                            {
                                Random rnd = new Random();
                                dnlib.DotNet.FieldAttributes fa =   dnlib.DotNet.FieldAttributes.HasFieldRVA | 
                                                                    dnlib.DotNet.FieldAttributes.InitOnly |
                                                                    dnlib.DotNet.FieldAttributes.Static |
                                                                    dnlib.DotNet.FieldAttributes.SpecialName;


                                FieldDef fieldToAdd =new FieldDefUser(string.Format("NewField{0}", rnd.Next()),null, fa);
                                currentMethod.Body.Instructions[i] = new Instruction(OpCodes.Ldtoken, fieldToAdd.Rid);
                                
                            }
                            //Put the field here
                            //currentMethod.Body.Instructions[i] = ;
                        }
                    }
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
