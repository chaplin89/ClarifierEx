using System.Collections.Generic;
using Clarifier.Core;
using dnlib.DotNet;
using System.Linq;
using System.IO;
using System.Reflection;
using System;
using System.Diagnostics;
using Clarifier.Identification.Impl;

namespace Clarifier.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Assert(args.Length > 0);
            ModuleDefMD targetModule = ModuleDefMD.Load(Directory.GetCurrentDirectory() + args[0]);
            ModuleDefMD runtimeModule = ModuleDefMD.Load(@".\Confuser.Runtime.dll");

            ClarifierContext ctx = new ClarifierContext() { CurrentModule = targetModule };

            AntiDumpIdentification antiDump = new AntiDumpIdentification();
            AntiDebugIdentification antiDebug = new AntiDebugIdentification();
            Constants constants = new Constants();
            AntiTamper antiTamper = new AntiTamper();


            antiTamper.Initialize();
            antiTamper.PerformIdentification(ctx);
            antiTamper.PerformRemoval(ctx);

            targetModule.NativeWrite(@".\Unobfuscated.exe");
            return;

            antiDump.Initialize(ctx);
            antiDump.PerformIdentification(ctx);
            antiDump.PerformRemoval(ctx);

            antiDebug.Initialize(ctx);
            antiDebug.PerformIdentification(ctx);
            antiDebug.PerformRemoval(ctx);

            constants.Initialize(ctx);
            constants.PerformIdentification(ctx);
            constants.PerformRemoval(ctx);

            //File.Delete(@"..\Obfuscated\Unobfuscated.exe");
        }
    }
}
