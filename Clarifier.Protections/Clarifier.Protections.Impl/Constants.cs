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
    public class Constants : BasicStaticProtection
    {
        Dictionary<long, TypeDef> sizeToArrayType = new Dictionary<long, TypeDef>();
        TypeDef ourType;
        TypeDefOrRefSig valueType;
        int unique = 0;

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
                                Type targetType = methodToInvoke.GetParameters()[parameters.Length - (i - j) - 1].ParameterType;
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
                                    parameters[parameters.Length - (i - j) - 1] = operand;
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
                                currentMethod.Body.Instructions[i] = new Instruction(OpCodes.Ldtoken, Create(returnedObject, ctx));
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

        public FieldDef Create(object data, IClarifierContext ctx)
        {
            int size = ((Array)data).Length;

            var arrayType = GetArrayType(size*4, ctx);
            var fieldSig = new FieldSig(new ValueTypeSig(arrayType));
            var attrs = dnlib.DotNet.FieldAttributes.Assembly | 
                        dnlib.DotNet.FieldAttributes.Static;
            var field = new FieldDefUser(string.Format("field_{0}", unique++), fieldSig, attrs);
            ctx.CurrentModule.UpdateRowId(field);
            field.HasFieldRVA = true;
            ourType.Fields.Add(field);
            var iv = new byte[size*4];
            Buffer.BlockCopy((Array)data, 0, iv, 0, size);
            field.InitialValue = iv;
            return field;
        }
    }
}
