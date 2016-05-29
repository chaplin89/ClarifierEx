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
using System.Threading;

namespace Clarifier.Identification.Impl
{
    public class Constants
    {
        BasicStaticProtection staticProtectionsManager = new BasicStaticProtection();
        Dictionary<long, TypeDef> sizeToArrayType = new Dictionary<long, TypeDef>();
        TypeDef ourType;
        TypeDefOrRefSig valueType;
        int unique = 0;

        public Constants()
        {
        }

        public bool Initialize(IClarifierContext ctx)
        {
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.Constant", "Get");
            staticProtectionsManager.AddPatternMatchingMethod("Confuser.Runtime.Constant", "Initialize");

            return staticProtectionsManager.LoadTypes();
        }

        public bool PerformRemoval(IClarifierContext ctx)
        {
            #region Move to Injecter
            byte[] newAssembly = ClarifierInjectHelper.GetBrandNewAssemblyFromType(ctx.CurrentModule.GlobalType);

            if (Debugger.IsAttached)
            {
                File.WriteAllBytes(@".\TestAssembly.dll", newAssembly);
            }
            Assembly asm = Assembly.Load(newAssembly);
            Type dummyType = asm.ManifestModule.GetType("DummyNamespace.DummyType");
            object dummyInstance = Activator.CreateInstance(dummyType);
            #endregion

            Dictionary<string, MethodInfo> mapNewMethodsToName = new Dictionary<string, MethodInfo>();
            List<MethodDef> onlyMethodsToSubstitute = staticProtectionsManager.DestinationMap.Where(x => x.name== "Get").First().matchingMethods;

            foreach (var v in staticProtectionsManager.DestinationMap)
            {
                foreach (var vv in v.matchingMethods)
                {
                    mapNewMethodsToName[vv.Name] = dummyType.GetMethod(vv.Name);
                }
            }

            foreach (var currentType in AllTypesHelper.Types(ctx.CurrentModule.Types))
            {
                foreach (var currentMethod in currentType.Methods)
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
                                    throw new NullReferenceException();
                                }


                                var v = ctx.CurrentModule.UpdateRowId(new TypeRefUser(ctx.CurrentModule, "System.Runtime.CompilerServices", "RuntimeHelpers", ctx.CurrentModule.CorLibTypes.AssemblyRef));
                                var classSignature = new ClassSig(v);
                                var vx = ctx.CurrentModule.UpdateRowId(new TypeRefUser(ctx.CurrentModule, "System", "Array", ctx.CurrentModule.CorLibTypes.AssemblyRef));
                                var vz = new ClassSig(vx);
                                var vy = ctx.CurrentModule.UpdateRowId(new TypeRefUser(ctx.CurrentModule, "System", "RuntimeFieldHandle", ctx.CurrentModule.CorLibTypes.AssemblyRef));
                                var vh = new ValueTypeSig(vy);
                                var methodSig = MethodSig.CreateStatic(ctx.CurrentModule.CorLibTypes.Void, vz, vh);
                                MemberRefUser mff = ctx.CurrentModule.UpdateRowId(new MemberRefUser(ctx.CurrentModule, "InitializeArray", methodSig, classSignature.TypeDefOrRef));

                                currentMethod.Body.Instructions[i] = new Instruction(OpCodes.Call, mff);
                                currentMethod.Body.Instructions.Insert(i, new Instruction(OpCodes.Ldtoken, Create((Array)returnedObject, ctx, elementSize)));
                                currentMethod.Body.Instructions.Insert(i, new Instruction(OpCodes.Dup));
                                currentMethod.Body.Instructions.Insert(i, new Instruction(OpCodes.Newarr,arrayType));
                                currentMethod.Body.Instructions.Insert(i, new Instruction(OpCodes.Ldc_I4, ((Array)returnedObject).Length));

                                    
                                // Ldc_I4
                                // Newarr
                                // Dup
                                // Ldtoken

                            }
                            else
                            {
                                Debugger.Break();
                            }
                        }
                    }
                }
            }

            return true;
        }

        public double PerformIdentification(IClarifierContext ctx)
        {
            return staticProtectionsManager.MapSourceInDestination(ctx.CurrentModule);
        }

        void CreateOurType(IClarifierContext ctx)
        {
            if (ourType != null)
                return;

            ourType = new TypeDefUser("", string.Format("<PrivateImplementationDetails>{0}", GetModuleId(ctx)), ctx.CurrentModule.CorLibTypes.Object.TypeDefOrRef);
            ourType.Attributes = dnlib.DotNet.TypeAttributes.NotPublic | dnlib.DotNet.TypeAttributes.AutoLayout |
                            dnlib.DotNet.TypeAttributes.Class | dnlib.DotNet.TypeAttributes.AnsiClass;
            ctx.CurrentModule.UpdateRowId(ourType);
            ctx.CurrentModule.Types.Add(ourType);
        }

        object GetModuleId(IClarifierContext ctx)
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);
            if (ctx.CurrentModule.Assembly != null)
                writer.Write(ctx.CurrentModule.Assembly.FullName);
            writer.Write((ctx.CurrentModule.Mvid ?? Guid.Empty).ToByteArray());
            var hash = new SHA1Managed().ComputeHash(memoryStream.GetBuffer());
            var guid = new Guid(BitConverter.ToInt32(hash, 0),
                                BitConverter.ToInt16(hash, 4),
                                BitConverter.ToInt16(hash, 6),
                                hash[8], hash[9], hash[10], hash[11],
                                hash[12], hash[13], hash[14], hash[15]);
            return guid.ToString("B");
        }

        TypeDef GetArrayType(long size, IClarifierContext ctx)
        {
            CreateOurType(ctx);

            TypeDef arrayType;
            if (sizeToArrayType.TryGetValue(size, out arrayType))
                return arrayType;

            if (valueType == null)
            {
                var typeRef = ctx.CurrentModule.UpdateRowId(new TypeRefUser(ctx.CurrentModule, "System", "ValueType", ctx.CurrentModule.CorLibTypes.AssemblyRef));
                valueType = new ClassSig(typeRef);
            }

            arrayType = new TypeDefUser("", string.Format("__StaticArrayInitTypeSize={0}", size), valueType.TypeDefOrRef);
            ctx.CurrentModule.UpdateRowId(arrayType);
            arrayType.Attributes =  dnlib.DotNet.TypeAttributes.NestedPrivate | 
                                    dnlib.DotNet.TypeAttributes.ExplicitLayout |
                                    dnlib.DotNet.TypeAttributes.Class | 
                                    dnlib.DotNet.TypeAttributes.Sealed | 
                                    dnlib.DotNet.TypeAttributes.AnsiClass;

            ourType.NestedTypes.Add(arrayType);
            sizeToArrayType[size] = arrayType;
            arrayType.ClassLayout = new ClassLayoutUser(1, (uint)size);
            return arrayType;
        }

        public FieldDef Create(Array data, IClarifierContext ctx, int elementSize)
        {
            int arrayLenght = data.Length;

            var arrayType = GetArrayType(arrayLenght * elementSize, ctx);
            var fieldSig = new FieldSig(new ValueTypeSig(arrayType));
            var attrs = dnlib.DotNet.FieldAttributes.Assembly | 
                        dnlib.DotNet.FieldAttributes.Static;

            var field = new FieldDefUser(string.Format("field_{0}", unique++), fieldSig, attrs);
            ctx.CurrentModule.UpdateRowId(field);
            field.HasFieldRVA = true;
            ourType.Fields.Add(field);
            var iv = new byte[arrayLenght * elementSize];
            Buffer.BlockCopy(data, 0, iv, 0, arrayLenght * elementSize);
            field.InitialValue = iv;
            return field;
        }
    }
}
