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

            List<MethodDef> blacklist = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Confuser.Runtime.AntiDebugSafe","Initialize"),
                new KeyValuePair<string, string>("Confuser.Runtime.AntiDebugSafe","Worker"),
                new KeyValuePair<string, string>("Confuser.Runtime.AntiDump","Initialize"),
            }.Select(x=> runtimeModule.Find(x.Key, true).FindMethod(x.Value)).ToList();

            List<MethodDef> toReplace = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Confuser.Runtime.Constant","Get"),
                new KeyValuePair<string, string>("Confuser.Runtime.Constant","Initialize")
            }.Select(x => runtimeModule.Find(x.Key, true).FindMethod(x.Value)).ToList();

            //BodyModifier.RemoveReferences(blacklist, targetModule);
            BodyModifier.FindAndReplaceWithResult(toReplace, targetModule);

            File.Delete(@"..\Obfuscated\Unobfuscated.exe");
            targetModule.Write(@"..\Obfuscated\Unobfuscated.exe");

        }
    }
}
