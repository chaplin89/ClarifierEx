using System.Collections.Generic;
using Clarifier.Core;
using dnlib.DotNet;
using System.Linq;
using System.IO;
using System.Reflection;
using System;
using System.Diagnostics;

namespace Clarifier.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Assert(args.Length > 0);
            ModuleDefMD targetModule = ModuleDefMD.Load(Directory.GetCurrentDirectory() + args[0]);
            ModuleDefMD runtimeModule = ModuleDefMD.Load(@".\Confuser.Runtime.dll");

            List<MethodDef> toReplace = new List<Tuple<string, string>>
            {
                Tuple.Create("Confuser.Runtime.Constant","Get"),
                //Tuple.Create("Confuser.Runtime.Constant","Initialize")
            }.Select(x => runtimeModule.Find(x.Item1, true).FindMethod(x.Item2)).ToList();

            BodyModifier.RemoveReferences(blacklist, targetModule);



            File.Delete(@"..\Obfuscated\Unobfuscated.exe");
            targetModule.Write(@"..\Obfuscated\Unobfuscated.exe");

        }
    }
}
