using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;
using System.IO;
using Confuser.Core.Helpers;

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
                                returnValue |= RemoveRefence(currentMethod, currentMethodToRemove);
                        }
                    }
                    NullifyMethod(currentMethodToRemove);
                }
            }
            return returnValue;
        }

        static public void FindAndReplaceWithResult(List<MethodDef> toFind, ModuleDefMD targetModule)
        {
            // First of all, a temporary assembly is created and methods are injected into this assembly.
            // Once this assembly is ready, this try to execute the method in order to replace all the references in
            // the original assembly with its result.
            // This is probably the weakest part of the deobfuscator but I doubt there's an easier way to do this.

            AssemblyDef asm = new AssemblyDefUser("DummyAssembly", new System.Version(1, 0, 0, 0), null);
            ModuleDef mod = new ModuleDefUser("DummyModule") { Kind = ModuleKind.Dll };
            TypeDef startUpType = new TypeDefUser("My.Namespace", "Startup", mod.CorLibTypes.Object.TypeDefOrRef);
            startUpType.Attributes = TypeAttributes.NotPublic | TypeAttributes.AutoLayout |
                                    TypeAttributes.Class | TypeAttributes.AnsiClass;

            mod.Types.Add(startUpType);
            asm.Modules.Add(mod);

            ClarifierInjectHelper.Inject(targetModule.GlobalType, startUpType, mod);

            //             MethodDef entryPoint = new MethodDefUser("Main",
            //                 MethodSig.CreateStatic(mod.CorLibTypes.Int32, new SZArraySig(mod.CorLibTypes.String)));
            // 
            //             entryPoint.Attributes = MethodAttributes.Private | MethodAttributes.Static |
            //                             MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
            //             entryPoint.ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;
            //             entryPoint.ParamDefs.Add(new ParamDefUser("args", 1));
            //             startUpType.Methods.Add(entryPoint);
            //             mod.EntryPoint = entryPoint;
            // 
            //             TypeRef consoleRef = new TypeRefUser(mod, "System", "Console", mod.CorLibTypes.AssemblyRef);
            //             MemberRef consoleWrite1 = new MemberRefUser(mod, "WriteLine",
            //                         MethodSig.CreateStatic(mod.CorLibTypes.Void, mod.CorLibTypes.String),
            //                         consoleRef);
            // 
            //             // Add a CIL method body to the entry point method
            //             CilBody epBody = new CilBody();
            //             entryPoint.Body = epBody;
            //             epBody.Instructions.Add(OpCodes.Ldstr.ToInstruction("Hello World!"));
            //             epBody.Instructions.Add(OpCodes.Call.ToInstruction(consoleWrite1));
            //             epBody.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
            //             epBody.Instructions.Add(OpCodes.Ret.ToInstruction());

            // Save the assembly to a file on disk
            MemoryStream ms = new MemoryStream();
            mod.Write(@"d:\chitemmuort.dll");
            mod.Write(ms);
            System.Reflection.Assembly asmReflection = System.Reflection.Assembly.Load(ms.GetBuffer());

            System.Type[] wtf= asmReflection.GetTypes();
            ms.Dispose();




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
