using Clarifier.Core;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Clarifier.Protection.Impl
{
    class ArrayType
    {
        int elementSize;
        ITypeDefOrRef typeDefOrRef;
    }
    /// <summary>
    /// This class manage the constants obfuscation.
    /// </summary>
    public class Constants
    {
        BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();
        ClarifierInjectHelper injectHelper = null;
        public Constants()
        {
        }

        public bool Initialize(ClarifierContext ctx)
        {
            injectHelper = new ClarifierInjectHelper(ctx);
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.Constant", "Get");
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.Constant", "Initialize");
            return staticProtectionsManager.LoadTypes();
        }

        public bool PerformRemoval(ClarifierContext ctx)
        {
            ClarifierInjectHelper inject = new ClarifierInjectHelper(ctx);
            object instantiatedObject = inject.CloneAndInstantiateType(ctx.CurrentModule.GlobalType);
            Type dummyType = instantiatedObject.GetType();

            Dictionary<string, MethodInfo> mapNewMethodsToName = new Dictionary<string, MethodInfo>();
            List<MethodDef> onlyMethodsToSubstitute = staticProtectionsManager.DestinationMap.Where(x => x.name == "Get").First().matchingMethods;

            foreach (var v in staticProtectionsManager.DestinationMap)
            {
                foreach (var vv in v.matchingMethods)
                {
                    mapNewMethodsToName[vv.Name] = dummyType.GetMethod(vv.Name);
                }
            }

            foreach (var currentMethod in ctx.CurrentModule.GetMethods())
            {
                if (onlyMethodsToSubstitute.Exists(x => x == currentMethod))
                    continue;

                if (!currentMethod.HasBody)
                    continue;

                // Look for calls to blacklisted methods
                for (var i = 0; i < currentMethod.Body.Instructions.Count; ++i)
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
                        // Here we are sure we are in presence of a call to a blacklisted methods;
                        // Kill ye olde damn bastard!
                        int inputParameters = methodToInvoke.GetParameters().Count();
                        object[] parameters = new object[inputParameters];

                        // Close the generic type before invoking
                        if (methodToInvoke.IsGenericMethod)
                        {
                            MethodSpec genericMethod = (MethodSpec)targetMethod;
                            Type[] genericTypes = genericMethod.GenericInstMethodSig.GenericArguments.Select(x => Type.GetType(x.ReflectionFullName)).ToArray();
                            methodToInvoke = methodToInvoke.MakeGenericMethod(genericTypes);
                        }

                        // Iterate backward in order to retrieve input parameters and removing instructions.
                        int j = i;
                        for (; j > i - inputParameters; j--)
                        {
                            Type targetType = methodToInvoke.GetParameters()[parameters.Length - (i - j) - 1].ParameterType;
                            object operand = currentMethod.Body.Instructions[j - 1].Operand;

                            if (targetType.IsValueType)
                            {
                                try
                                {
                                    // Most common situation here is that the operand is an uint.
                                    // dnlib fail to assign the right type (int instead of uint)
                                    // so we force the cast...
                                    parameters[parameters.Length - (i - j) - 1] = (uint)(int)operand;
                                }
                                catch
                                {
                                    // ...If the cast fail we try a conversion with value semantic. If even
                                    // this cast fail, we lift the white flag. 
                                    parameters[parameters.Length - (i - j) - 1] = Convert.ChangeType(operand, targetType);
                                }
                            }
                            else
                            {
                                parameters[parameters.Length - (i - j) - 1] = operand;
                            }

                            currentMethod.Body.Instructions.RemoveAt(j - 1);
                        }
                        i = j;

                        object returnedObject = methodToInvoke.Invoke(null, parameters);
                        Type returnedType = returnedObject.GetType();

                        if (returnedType == typeof(string))
                        {
                            currentMethod.Body.Instructions[i] = new Instruction(OpCodes.Ldstr, returnedObject);
                        }
                        else if (returnedType.IsArray)
                        {
                            ITypeDefOrRef arrayType = null;
                            int elementSize = 0;
                            if (returnedType.Name == typeof(int[]).Name || returnedType.Name == typeof(float[]).Name)
                            {
                                elementSize = 4;
                                if (returnedType.Name == typeof(int[]).Name)
                                    arrayType = ctx.CurrentModule.CorLibTypes.Int32.TypeDefOrRef;
                                else
                                    arrayType = ctx.CurrentModule.CorLibTypes.Double.TypeDefOrRef;
                            }
                            else if (returnedType.Name == typeof(long[]).Name || returnedType.Name == typeof(double[]).Name)
                            {
                                elementSize = 8;

                                if (returnedType.Name == typeof(long[]).Name)
                                    arrayType = ctx.CurrentModule.CorLibTypes.Int64.TypeDefOrRef;
                                else
                                    arrayType = ctx.CurrentModule.CorLibTypes.Double.TypeDefOrRef;
                            }
                            else
                            {
                                Debugger.Break();
                            }

                            injectHelper.InjectArray((Array)returnedObject, currentMethod, arrayType, elementSize, i);
                        }
                        else
                        {
                            Debugger.Break();
                        }
                    }
                }
            }

            return true;
        }

        public double PerformIdentification(ClarifierContext ctx)
        {
            return staticProtectionsManager.MapSourceInDestination(ctx.CurrentModule);
        }
    }
}
