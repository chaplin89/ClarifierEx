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
            ModuleDefMD targetModule = ModuleDefMD.Load(@"..\Obfuscated\ConsoleTest.exe");
            ModuleDefMD confuserRuntimeModule = ModuleDefMD.Load(@".\Confuser.Runtime.dll");
            PlasticSurgeon surgeon = new PlasticSurgeon();

//             MemoryStream sw = new MemoryStream();
//             AssemblyDefUser newAssembly = new AssemblyDefUser("TempAssembly");
//             ModuleDefUser newModule = new ModuleDefUser("TempModule");
//             TypeDefUser newType = new TypeDefUser("TempModule.TempType");
//             ModuleDefMD mscorlibDef = ModuleDefMD.Load(typeof(object).Module);
//             ModuleRefUser mscorlibRef = new ModuleRefUser(mscorlibDef);
//             
//             TypeRefUser trUser = new TypeRefUser(mscorlibDef, typeof(object).FullName);
//             trUser.ResolutionScope = mscorlibRef;
//             TypeDef md = mscorlibDef.Find(typeof(object).FullName, true);
//             trUser.Name = "mscorlib";
//             newType.BaseType= trUser;
//             
//             newModule.Types.Add(newType);
//             newAssembly.Modules.Add(newModule);
// 
//             newAssembly.Write(@".\TestNewAssembly.dll");

            List<KeyValuePair<string, string>> blacklist = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Confuser.Runtime.AntiDebugSafe","Initialize"),
                new KeyValuePair<string, string>("Confuser.Runtime.AntiDebugSafe","Worker"),
                new KeyValuePair<string, string>("Confuser.Runtime.AntiDump","Initialize"),
            };

            List<KeyValuePair<string, string>> toReplace = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Confuser.Runtime.Constant","Get"),
                new KeyValuePair<string, string>("Confuser.Runtime.Constant","Initialize")
            };

            surgeon.RemoveReferences(confuserRuntimeModule, blacklist, targetModule);
            surgeon.ReplaceWithResult(confuserRuntimeModule, toReplace, targetModule);

            File.Delete(@"..\Obfuscated\Unobfuscated.exe");
            targetModule.Write(@"..\Obfuscated\Unobfuscated.exe");

        }
    }
}
