using dnlib.DotNet;
using System.Diagnostics;
using Clarifier.Identification.Impl;

namespace Clarifier.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Assert(args.Length > 0);
            ModuleDefMD targetModule = ModuleDefMD.Load(args[0]);
            ModuleDefMD runtimeModule = ModuleDefMD.Load("Confuser.Runtime.dll");

            ClarifierContext ctx = new ClarifierContext { CurrentModule = targetModule };

            AntiDumpIdentification antiDump = new AntiDumpIdentification();
            AntiDebugIdentification antiDebug = new AntiDebugIdentification();
            Constants constants = new Constants();
            AntiTamper antiTamper = new AntiTamper();
            Inliner inliner = new Inliner();

            inliner.PerformIdentification(ctx);
            inliner.PerformRemoval(ctx);

            //             antiTamper.Initialize();
            //             antiTamper.PerformIdentification(ctx);
            //             antiTamper.PerformRemoval(ctx);
            // 
            //             antiDump.Initialize(ctx);
            //             antiDump.PerformIdentification(ctx);
            //             antiDump.PerformRemoval(ctx);
            // 
            //             antiDebug.Initialize(ctx);
            //             antiDebug.PerformIdentification(ctx);
            //             antiDebug.PerformRemoval(ctx);
            // 
            //             constants.Initialize(ctx);
            //             constants.PerformIdentification(ctx);
            //             constants.PerformRemoval(ctx);

            int lastBackslash = args[0].LastIndexOf('\\');
            int secondLastBackslash = args[0].LastIndexOf('\\', lastBackslash - 1);
            string targetPath = args[0].Substring(secondLastBackslash+1,lastBackslash-1-secondLastBackslash);
            string targetExecutable = args[0].Substring(lastBackslash+1, args[0].Length-1-lastBackslash);
            string destinationFile = args[0].Replace(targetPath, "Deobfuscated");

            targetModule.Write(destinationFile);
            return;
        }
    }
}
