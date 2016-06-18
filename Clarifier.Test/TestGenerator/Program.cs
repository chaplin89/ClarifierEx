using System;
using System.Collections.Generic;
using Confuser.Core.Project;
using System.Xml;
using Confuser.Core;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Clarifier.Test.TestGenerator
{
    internal class ModuleDescriptor
    {
        public string inputFileName;
        public Dictionary<ProtectionType, Dictionary<string, string>> protections;
        public string outputFileName;
        public ModuleDescriptor(string inputFileName)
        {
            this.inputFileName = inputFileName;
            outputFileName = "Obfuscated" + inputFileName;
            protections = new Dictionary<ProtectionType, Dictionary<string, string>>();
        }
    }

    class Program
    {
        static string outputPath = Path.Combine(Directory.GetCurrentDirectory(),"Obfuscated");
        static string inputPath = Path.Combine(Directory.GetCurrentDirectory(),"Unobfuscated");
        static string outputProject = Path.Combine(Directory.GetCurrentDirectory(),"OutputConfuserProject.crproj");
        static bool invokeObfuscator = true;
        static string confuserPath;

        static List<ModuleDescriptor> registeredApplications = new List<ModuleDescriptor>()
        {
            new ModuleDescriptor("SimpleConsoleApplication.exe")
            {
                protections = new Dictionary<ProtectionType, Dictionary<string, string>>
                {
                    {ProtectionType.Constants, new Dictionary<string,string>{ { "mode", "dynamic" } } },
                    //{ProtectionType.Rename, new Dictionary<string, string> { { "mode", "debug" } } }
                    //{ ProtectionType.AntiDebug,null },
                    //{ ProtectionType.AntiDump, null},
                    { ProtectionType.ReferenceProxy, new Dictionary<string,string>{ { "mode", "mild" } }},
                    //{ ProtectionType.ControlFlow, null}
                }
            }
        };

        static void Main(string[] args)
        {
            //This is reserved for the moment that a well structured test will be needed.
            ConfuserProtection.MapProtectionType = JsonConvert.DeserializeObject<Dictionary<ProtectionType, ProtectionDescription>>(File.ReadAllText("SettingsDescription.json"));
            
            if (args.Length > 0)
                confuserPath = Path.Combine(args[0],"Confuser.CLI.exe");
            if (args.Length > 1)
                inputPath = args[1];
            if (args.Length > 2)
                outputPath = args[2];
            if (args.Length > 3)
                outputProject = args[3];
            if (args.Length > 4)
                bool.TryParse(args[4], out invokeObfuscator);

            try { 
                Directory.CreateDirectory(inputPath);
                Directory.CreateDirectory(outputPath);
                Directory.GetParent(outputProject).Create();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to create directory: {0}", ex.ToString());
                return;
            }

            Debug.Assert(Directory.Exists(inputPath));
            Debug.Assert(Directory.Exists(outputPath));
            Debug.Assert(Directory.GetParent(outputProject).Exists);
            Debug.Assert(!invokeObfuscator  || File.Exists(confuserPath));

            ConfuserProject module = new ConfuserProject()
            {
                OutputDirectory = outputPath,
                BaseDirectory = inputPath
            };

            foreach (var v in registeredApplications)
            {
                Console.WriteLine("Processing {0}", v.inputFileName);
                ProjectModule project = new ProjectModule() { Path = v.inputFileName };
                Rule moduleRule = new Rule();

                foreach (var vv in v.protections)
                {
                    string protection = ConfuserProtection.MapProtectionType[vv.Key].Name;
                    SettingItem<Protection> currentProtection = new SettingItem<Protection>(protection);

                    if (vv.Value != null)
                    {
                        foreach (var vvv in vv.Value)
                            currentProtection.Add(vvv.Key, vvv.Value);
                    }
                    moduleRule.Add(currentProtection);
                    Console.WriteLine("\tAdded protection: {0}", protection);
                }

                project.Rules.Add(moduleRule);
                module.Add(project);
            }

            Console.WriteLine("Saving project: {0}", outputProject);
            XmlDocument document = module.Save();

            using (XmlWriter xmlWrite = XmlWriter.Create(outputProject))
                document.WriteContentTo(xmlWrite);

            if (invokeObfuscator)
            {
                Console.WriteLine("");
                Console.WriteLine("Invoke obfuscator required. Invoking.");
                Process.Start(confuserPath, outputProject).WaitForExit();
                Console.WriteLine("Done. Exiting.");
            }
        }
    }
}
