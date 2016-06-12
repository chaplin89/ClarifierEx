using Confuser.Core;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Clarifier.Core
{
    /// <summary>
    ///     Provides methods to inject a <see cref="TypeDef" /> into another module.
    ///     This is an improved version of the InjectHelper present in ConfuserEx.
    /// </summary>
    public class ClarifierInjectHelper
    {
        Dictionary<long, TypeDef> sizeToArrayType = new Dictionary<long, TypeDef>();
        TypeDef ourType;
        TypeDefOrRefSig valueType;
        int unique = 0;
        Dictionary<MethodDef, dnlib.DotNet.Writer.MethodBody> code = new Dictionary<MethodDef, dnlib.DotNet.Writer.MethodBody>();
        ClarifierContext ctx;
        private bool eventAdded = false;
        #region "Array inject helper"
        public void InjectArray(Array arrayToInject, MethodDef method, ITypeDefOrRef arrayType, int elementSize, int instructionIndex)
        {
            var runtimeHelpersRef = method.Module.UpdateRowId(new TypeRefUser(method.Module, "System.Runtime.CompilerServices", "RuntimeHelpers", method.Module.CorLibTypes.AssemblyRef));
            var runtimeHelpersSig = new ClassSig(runtimeHelpersRef);
            var arrayRef = method.Module.UpdateRowId(new TypeRefUser(method.Module, "System", "Array", method.Module.CorLibTypes.AssemblyRef));
            var arrayRefSig = new ClassSig(arrayRef);
            var runtimeFieldHandleRef = method.Module.UpdateRowId(new TypeRefUser(method.Module, "System", "RuntimeFieldHandle", method.Module.CorLibTypes.AssemblyRef));
            var RuntimeFieldHandleSig = new ValueTypeSig(runtimeFieldHandleRef);
            var initArraySig = MethodSig.CreateStatic(method.Module.CorLibTypes.Void, arrayRefSig, RuntimeFieldHandleSig);
            MemberRefUser initArrayRef = method.Module.UpdateRowId(new MemberRefUser(method.Module, "InitializeArray", initArraySig, runtimeHelpersSig.TypeDefOrRef));

            // The following will insert these instructions:
            // Ldc_I4 arraySize
            // Newarr arrayType
            // Dup
            // Ldtoken initArrayToken

            method.Body.Instructions[instructionIndex] = new Instruction(OpCodes.Call, initArrayRef);
            method.Body.Instructions.Insert(instructionIndex, new Instruction(OpCodes.Ldtoken, Create(arrayToInject, method.Module, elementSize)));
            method.Body.Instructions.Insert(instructionIndex, new Instruction(OpCodes.Dup));
            method.Body.Instructions.Insert(instructionIndex, new Instruction(OpCodes.Newarr, arrayType));
            method.Body.Instructions.Insert(instructionIndex, new Instruction(OpCodes.Ldc_I4, arrayToInject.Length));
        }
        void CreateOurType(ModuleDef ctx)
        {
            if (ourType != null)
                return;

            ourType = new TypeDefUser("", string.Format("<PrivateImplementationDetails>{0}", GetModuleId(ctx)), ctx.CorLibTypes.Object.TypeDefOrRef);
            ourType.Attributes = dnlib.DotNet.TypeAttributes.NotPublic | dnlib.DotNet.TypeAttributes.AutoLayout |
                            dnlib.DotNet.TypeAttributes.Class | dnlib.DotNet.TypeAttributes.AnsiClass;
            ctx.UpdateRowId(ourType);
            ctx.Types.Add(ourType);
        }

        object GetModuleId(ModuleDef ctx)
        {
            var memoryStream = new MemoryStream();
            var writer = new BinaryWriter(memoryStream);
            if (ctx.Assembly != null)
                writer.Write(ctx.Assembly.FullName);
            writer.Write((ctx.Mvid ?? Guid.Empty).ToByteArray());
            var hash = new System.Security.Cryptography.SHA1Managed().ComputeHash(memoryStream.GetBuffer());
            var guid = new Guid(BitConverter.ToInt32(hash, 0),
                                BitConverter.ToInt16(hash, 4),
                                BitConverter.ToInt16(hash, 6),
                                hash[8], hash[9], hash[10], hash[11],
                                hash[12], hash[13], hash[14], hash[15]);
            return guid.ToString("B");
        }

        TypeDef GetArrayType(long size, ModuleDef ctx)
        {
            CreateOurType(ctx);

            TypeDef arrayType;
            if (sizeToArrayType.TryGetValue(size, out arrayType))
                return arrayType;

            if (valueType == null)
            {
                var typeRef = ctx.UpdateRowId(new TypeRefUser(ctx, "System", "ValueType", ctx.CorLibTypes.AssemblyRef));
                valueType = new ClassSig(typeRef);
            }

            arrayType = new TypeDefUser("", string.Format("__StaticArrayInitTypeSize={0}", size), valueType.TypeDefOrRef);
            ctx.UpdateRowId(arrayType);
            arrayType.Attributes = dnlib.DotNet.TypeAttributes.NestedPrivate |
                                    dnlib.DotNet.TypeAttributes.ExplicitLayout |
                                    dnlib.DotNet.TypeAttributes.Class |
                                    dnlib.DotNet.TypeAttributes.Sealed |
                                    dnlib.DotNet.TypeAttributes.AnsiClass;

            ourType.NestedTypes.Add(arrayType);
            sizeToArrayType[size] = arrayType;
            arrayType.ClassLayout = new ClassLayoutUser(1, (uint)size);
            return arrayType;
        }

        FieldDef Create(Array data, ModuleDef ctx, int elementSize)
        {
            int arrayLenght = data.Length;

            var arrayType = GetArrayType(arrayLenght * elementSize, ctx);
            var fieldSig = new FieldSig(new ValueTypeSig(arrayType));
            var attrs = dnlib.DotNet.FieldAttributes.Assembly |
                        dnlib.DotNet.FieldAttributes.Static;

            var field = new FieldDefUser(string.Format("field_{0}", unique++), fieldSig, attrs);
            ctx.UpdateRowId(field);
            field.HasFieldRVA = true;
            ourType.Fields.Add(field);
            var iv = new byte[arrayLenght * elementSize];
            Buffer.BlockCopy(data, 0, iv, 0, arrayLenght * elementSize);
            field.InitialValue = iv;
            return field;
        }
        #endregion

        public ClarifierInjectHelper (ClarifierContext ctx)
        {
            this.ctx = ctx;
        }

        #region Cloning services
        /// <summary>
        ///     Clones the specified origin TypeDef.
        /// </summary>
        /// <param name="origin">The origin TypeDef.</param>
        /// <returns>The cloned TypeDef.</returns>
        static TypeDefUser Clone(TypeDef origin)
        {
            var ret = new TypeDefUser(origin.Namespace, origin.Name);
            ret.Attributes = origin.Attributes;

            if (origin.ClassLayout != null)
                ret.ClassLayout = new ClassLayoutUser(origin.ClassLayout.PackingSize, origin.ClassSize);

            foreach (GenericParam genericParam in origin.GenericParameters)
                ret.GenericParameters.Add(new GenericParamUser(genericParam.Number, genericParam.Flags, "-"));

            return ret;
        }

        /// <summary>
        ///     Clones the specified origin MethodDef.
        /// </summary>
        /// <param name="origin">The origin MethodDef.</param>
        /// <returns>The cloned MethodDef.</returns>
        static MethodDefUser Clone(MethodDef origin)
        {
            string name;
            if (origin.IsSpecialName)
                name = origin.Name;
            else
                name = origin.Name;// string.Format("Method{0}", ixxx++);

            var ret = new MethodDefUser(name, null, origin.ImplAttributes, origin.Attributes);

            foreach (GenericParam genericParam in origin.GenericParameters)
                ret.GenericParameters.Add(new GenericParamUser(genericParam.Number, genericParam.Flags, "-"));

            ret.Access = MethodAttributes.Public;
            return ret;
        }

        /// <summary>
        ///     Clones the specified origin FieldDef.
        /// </summary>
        /// <param name="origin">The origin FieldDef.</param>
        /// <returns>The cloned FieldDef.</returns>
        static FieldDefUser Clone(FieldDef origin)
        {
            var ret = new FieldDefUser(origin.Name, null, origin.Attributes);
            ret.HasFieldRVA = origin.HasFieldRVA;
            return ret;
        }

        public unsafe object CloneAndInstantiateType(TypeDef typeToInstantiate)
        {
            byte[] newAssembly = GetBrandNewAssemblyFromType(typeToInstantiate);

            if (Debugger.IsAttached)
            {
                if (File.Exists("DumpNewAssembly"))
                    File.WriteAllBytes("TestAssembly.dll", newAssembly);
            }

            System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(newAssembly);
            Type dummyType = assembly.ManifestModule.GetType("DummyNamespace.DummyType");
            object dummyInstance = Activator.CreateInstance(dummyType);
            return dummyInstance;
        }

        public byte[] GetBrandNewAssemblyFromType(TypeDef typeToInject)
        {
            // First of all, a temporary assembly is created and methods are injected into this assembly.
            // Once this assembly is ready, this try to execute the method in order to replace all the references in
            // the original assembly with its result.
            // This is probably the weakest part of the deobfuscator but I doubt there's an easier way to do this.

            AssemblyDef dummyAssembly = new AssemblyDefUser("DummyAssembly", new System.Version(1, 0, 0, 0), null);
            ModuleDefUser dummyModule = new ModuleDefUser("DummyModule") { Kind = ModuleKind.Dll };
            TypeDef dummyType = new TypeDefUser("DummyNamespace", "DummyType", dummyModule.CorLibTypes.Object.TypeDefOrRef);
            dummyType.Attributes = TypeAttributes.NotPublic | TypeAttributes.AutoLayout |
                                    TypeAttributes.Class | TypeAttributes.AnsiClass;

            dummyModule.Types.Add(dummyType);
            dummyAssembly.Modules.Add(dummyModule);

            // Copy everything in dummyType
            Inject(typeToInject, dummyType, dummyModule, null);

            // Provide a default constructor
            if (dummyType.FindDefaultConstructor() == null)
            {
                var ctor = new MethodDefUser(".ctor",
                    MethodSig.CreateInstance(dummyModule.CorLibTypes.Void),
                    MethodImplAttributes.Managed,
                    MethodAttributes.HideBySig | MethodAttributes.Public |
                    MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                ctor.Body = new CilBody();
                ctor.Body.MaxStack = 0;
                ctor.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                dummyType.Methods.Add(ctor);
            }

            // Save the assembly to a memorystream
            using (MemoryStream stream = new MemoryStream())
            {
                dummyModule.Write(stream);
                return stream.ToArray();
            }
        }

        void InjectNativeCode(object sender, ModuleWriterEvent e)
        {
            var writer = (ModuleWriterBase)sender;
            if (e == ModuleWriterEvent.MDBeginWriteMethodBodies)
            {
                for (var v = 0; v<code.Keys.Count; v++)
                { 
                    code[code.Keys.ElementAt(v)] = writer.MethodBodies.Add(code[code.Keys.ElementAt(v)]);
                }
            }
            else if (e  == ModuleWriterEvent.EndCalculateRvasAndFileOffsets)
            {
                for (var v = 0; v < code.Keys.Count; v++)
                {
                    uint rid = writer.MetaData.GetRid(code.Keys.ElementAt(v));
                    writer.MetaData.TablesHeap.MethodTable[rid].RVA = (uint)code[code.Keys.ElementAt(v)].RVA;
                }
            }
        }

        /// <summary>
        ///     Populates the context mappings.
        /// </summary>
        /// <param name="typeDef">The origin TypeDef.</param>
        /// <param name="ctx">The injection context.</param>
        /// <returns>The new TypeDef.</returns>
        static TypeDef PopulateContext(TypeDef typeDef, InjectContext ctx)
        {
            TypeDef ret;
            IDnlibDef existing;
            if (!ctx.Map.TryGetValue(typeDef, out existing))
            {
                ret = Clone(typeDef);
                ctx.Map[typeDef] = ret;
            }
            else
                ret = (TypeDef)existing;

            foreach (TypeDef nestedType in typeDef.NestedTypes)
                ret.NestedTypes.Add(PopulateContext(nestedType, ctx));

            foreach (MethodDef method in typeDef.Methods)
            {
                if (ctx.Filter != null && !ctx.Filter.Contains(method))
                    continue;

                ret.Methods.Add((MethodDef)(ctx.Map[method] = Clone(method)));
            }

            foreach (FieldDef field in typeDef.Fields)
                ret.Fields.Add((FieldDef)(ctx.Map[field] = Clone(field)));

            return ret;
        }

        /// <summary>
        ///     Copies the information from the origin type to injected type.
        /// </summary>
        /// <param name="typeDef">The origin TypeDef.</param>
        /// <param name="ctx">The injection context.</param>
        static void CopyTypeDef(TypeDef typeDef, InjectContext ctx)
        {
            var newTypeDef = (TypeDef)ctx.Map[typeDef];

            newTypeDef.BaseType = (ITypeDefOrRef)ctx.Importer.Import(typeDef.BaseType);

            foreach (InterfaceImpl iface in typeDef.Interfaces)
                newTypeDef.Interfaces.Add(new InterfaceImplUser((ITypeDefOrRef)ctx.Importer.Import(iface.Interface)));
        }

        /// <summary>
        ///     Copies the information from the origin method to injected method.
        /// </summary>
        /// <param name="methodDef">The origin MethodDef.</param>
        /// <param name="ctx">The injection context.</param>
        void CopyMethodDef(MethodDef methodDef, InjectContext ctx)
        {
            if (ctx.Filter != null && !ctx.Filter.Contains(methodDef))
                return;

            var newMethodDef = (MethodDef)ctx.Map[methodDef];

            newMethodDef.Signature = ctx.Importer.Import(methodDef.Signature);
            newMethodDef.Parameters.UpdateParameterTypes();

            if (methodDef.ImplMap != null)
                newMethodDef.ImplMap = new ImplMapUser(new ModuleRefUser(ctx.TargetModule, methodDef.ImplMap.Module.Name), methodDef.ImplMap.Name, methodDef.ImplMap.Attributes);

            foreach (CustomAttribute ca in methodDef.CustomAttributes)
                newMethodDef.CustomAttributes.Add(new CustomAttribute((ICustomAttributeType)ctx.Importer.Import(ca.Constructor)));

            if (methodDef.CodeType == MethodImplAttributes.Native)
            {
                dnlib.PE.RVA methodRVA = methodDef.NativeBody.RVA;
                List<byte> methodBody = new List<byte>();
                
                ModuleDefMD moduleMD = (ModuleDefMD)methodDef.Module;
                var stream = moduleMD.MetaData.PEImage.CreateStream(moduleMD.MetaData.PEImage.ToFileOffset(methodRVA));
                byte byteToAdd;
                do
                {
                    byteToAdd = stream.ReadByte();
                    methodBody.Add(byteToAdd);
                } while (byteToAdd != 0xc3);

                code[newMethodDef] = new dnlib.DotNet.Writer.MethodBody(methodBody.ToArray());
                
                if (!eventAdded)
                {
                    this.ctx.WriterListener.OnWriter += InjectNativeCode;
                    eventAdded = true;
                }
                return;
            }

            if (methodDef.HasBody)
            {
                newMethodDef.Body = new CilBody(methodDef.Body.InitLocals, new List<Instruction>(), new List<ExceptionHandler>(), new List<Local>());
                newMethodDef.Body.MaxStack = methodDef.Body.MaxStack;

                var bodyMap = new Dictionary<object, object>();

                foreach (Local local in methodDef.Body.Variables)
                {
                    var newLocal = new Local(ctx.Importer.Import(local.Type));
                    newMethodDef.Body.Variables.Add(newLocal);
                    newLocal.Name = local.Name;
                    newLocal.PdbAttributes = local.PdbAttributes;

                    bodyMap[local] = newLocal;
                }

                foreach (Instruction instr in methodDef.Body.Instructions)
                {
                    var newInstr = new Instruction(instr.OpCode, instr.Operand);
                    newInstr.SequencePoint = instr.SequencePoint;

                    if (newInstr.Operand is IType)
                        newInstr.Operand = ctx.Importer.Import((IType)newInstr.Operand);

                    else if (newInstr.Operand is IMethod)
                        newInstr.Operand = ctx.Importer.Import((IMethod)newInstr.Operand);

                    else if (newInstr.Operand is IField)
                        newInstr.Operand = ctx.Importer.Import((IField)newInstr.Operand);

                    newMethodDef.Body.Instructions.Add(newInstr);
                    bodyMap[instr] = newInstr;
                }

                foreach (Instruction instr in newMethodDef.Body.Instructions)
                {
                    if (instr.Operand != null && bodyMap.ContainsKey(instr.Operand))
                        instr.Operand = bodyMap[instr.Operand];

                    else if (instr.Operand is Instruction[])
                        instr.Operand = ((Instruction[])instr.Operand).Select(target => (Instruction)bodyMap[target]).ToArray();
                }

                foreach (ExceptionHandler eh in methodDef.Body.ExceptionHandlers)
                    newMethodDef.Body.ExceptionHandlers.Add(new ExceptionHandler(eh.HandlerType)
                    {
                        CatchType = eh.CatchType == null ? null : (ITypeDefOrRef)ctx.Importer.Import(eh.CatchType),
                        TryStart = (Instruction)bodyMap[eh.TryStart],
                        TryEnd = (Instruction)bodyMap[eh.TryEnd],
                        HandlerStart = (Instruction)bodyMap[eh.HandlerStart],
                        HandlerEnd = (Instruction)bodyMap[eh.HandlerEnd],
                        FilterStart = eh.FilterStart == null ? null : (Instruction)bodyMap[eh.FilterStart]
                    });

                newMethodDef.Body.SimplifyMacros(newMethodDef.Parameters);
            }
        }

        /// <summary>
        ///     Copies the information from the origin field to injected field.
        /// </summary>
        /// <param name="fieldDef">The origin FieldDef.</param>
        /// <param name="ctx">The injection context.</param>
        void CopyFieldDef(FieldDef fieldDef, InjectContext ctx)
        {
            var newFieldDef = (FieldDef)ctx.Map[fieldDef];

            newFieldDef.Signature = ctx.Importer.Import(fieldDef.Signature);
            if (newFieldDef.HasFieldRVA)
                newFieldDef.InitialValue = fieldDef.InitialValue;
        }

        /// <summary>
        ///     Copies the information to the injected definitions.
        /// </summary>
        /// <param name="typeDef">The origin TypeDef.</param>
        /// <param name="ctx">The injection context.</param>
        /// <param name="copySelf">if set to <c>true</c>, copy information of <paramref name="typeDef" />.</param>
        void Copy(TypeDef typeDef, InjectContext ctx, bool copySelf)
        {
            if (copySelf)
                CopyTypeDef(typeDef, ctx);

            foreach (TypeDef nestedType in typeDef.NestedTypes)
                Copy(nestedType, ctx, true);

            foreach (MethodDef method in typeDef.Methods)
                CopyMethodDef(method, ctx);

            foreach (FieldDef field in typeDef.Fields)
                CopyFieldDef(field, ctx);
        }

        /// <summary>
        ///     Injects the specified TypeDef to another module.
        /// </summary>
        /// <param name="typeDef">The source TypeDef.</param>
        /// <param name="target">The target module.</param>
        /// <returns>The injected TypeDef.</returns>
        public TypeDef Inject(TypeDef typeDef, ModuleDef target)
        {
            var ctx = new InjectContext(typeDef.Module, target);
            PopulateContext(typeDef, ctx);
            Copy(typeDef, ctx, true);
            return (TypeDef)ctx.Map[typeDef];
        }

        /// <summary>
        ///     Injects the specified MethodDef to another module.
        /// </summary>
        /// <param name="methodDef">The source MethodDef.</param>
        /// <param name="target">The target module.</param>
        /// <returns>The injected MethodDef.</returns>
        public MethodDef Inject(MethodDef methodDef, ModuleDef target)
        {
            var ctx = new InjectContext(methodDef.Module, target);
            ctx.Map[methodDef] = Clone(methodDef);
            CopyMethodDef(methodDef, ctx);
            return (MethodDef)ctx.Map[methodDef];
        }

        /// <summary>
        ///     Injects the members of specified TypeDef to another module.
        /// </summary>
        /// <param name="typeDef">The source TypeDef.</param>
        /// <param name="newType">The new type.</param>
        /// <param name="target">The target module.</param>
        /// <returns>Injected members.</returns>
        public IEnumerable<IDnlibDef> Inject(TypeDef typeDef, TypeDef newType, ModuleDef target, List<MethodDef> filter)
        {
            var ctx = new InjectContext(typeDef.Module, target) { Filter = filter };
            ctx.Map[typeDef] = newType;
            PopulateContext(typeDef, ctx);
            Copy(typeDef, ctx, false);
            return ctx.Map.Values.Except(new[] { newType });
        }

        /// <summary>
        ///     Context of the injection process.
        /// </summary>
        class InjectContext : ImportResolver
        {
            /// <summary>
            ///     The mapping of origin definitions to injected definitions.
            /// </summary>
            public readonly Dictionary<IDnlibDef, IDnlibDef> Map = new Dictionary<IDnlibDef, IDnlibDef>();

            /// <summary>
            ///     The module which source type originated from.
            /// </summary>
            public readonly ModuleDef OriginModule;

            /// <summary>
            ///     The module which source type is being injected to.
            /// </summary>
            public readonly ModuleDef TargetModule;

            /// <summary>
            ///     The importer.
            /// </summary>
            readonly Importer importer;

            /// <summary>
            ///     Initializes a new instance of the <see cref="InjectContext" /> class.
            /// </summary>
            /// <param name="module">The origin module.</param>
            /// <param name="target">The target module.</param>
            public InjectContext(ModuleDef module, ModuleDef target)
            {
                OriginModule = module;
                TargetModule = target;
                importer = new Importer(target, ImporterOptions.TryToUseTypeDefs);
                importer.Resolver = this;
                Filter = null;
            }

            public List<MethodDef> Filter { get; internal set; }

            /// <summary>
            ///     Gets the importer.
            /// </summary>
            /// <value>The importer.</value>
            public Importer Importer
            {
                get { return importer; }
            }

            /// <inheritdoc />
            public override TypeDef Resolve(TypeDef typeDef)
            {
                if (Map.ContainsKey(typeDef))
                    return (TypeDef)Map[typeDef];
                return null;
            }

            /// <inheritdoc />
            public override MethodDef Resolve(MethodDef methodDef)
            {
                if (Map.ContainsKey(methodDef))
                    return (MethodDef)Map[methodDef];
                return null;
            }

            /// <inheritdoc />
            public override FieldDef Resolve(FieldDef fieldDef)
            {
                if (Map.ContainsKey(fieldDef))
                    return (FieldDef)Map[fieldDef];
                return null;
            }
        }
        #endregion
    }
}