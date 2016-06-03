using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Clarifier.Core
{
    public static class DnlibExtensionMethods
    {
        public static IList<Instruction> GetInstructions(this MethodDef method)
        {
            if (!method.HasBody)
                return new List<Instruction>();
            if (!method.IsIL)
                return new List<Instruction>();

            return method.Body.Instructions;
        }

        public static IEnumerable<MethodDef> GetMethods(this ModuleDef module)
        {
            foreach(var type in AllTypesHelper.Types(module.Types))
            {
                foreach (var method in type.Methods)
                    yield return method;
            }
        }
    }
}
