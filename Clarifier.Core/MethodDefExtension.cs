using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Clarifier.Core
{
    public static class MethodDefExtension
    {
        public static IEnumerable<Instruction> GetInstruction(this MethodDef method)
        {
            if (!method.HasBody)
                yield break;
            if (!method.IsIL)
                yield break;

            foreach (var v in method.Body.Instructions)
                yield return v;
        }
    }
}
