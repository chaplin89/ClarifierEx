using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confuser.Core.Project;
using System.Xml;
using Confuser.Core;
using System.IO;
using System.Diagnostics;

namespace Clarifier.Test.TestGenerator
{
    internal class ModuleDescriptor
    {
        public string inputFileName;
        public List<ProtectionType> protections;
        public string outputFileName;
        public ModuleDescriptor(string inputFileName)
        {
            this.inputFileName = inputFileName;
            outputFileName = "Obfuscated" + inputFileName;
            protections = new List<ProtectionType>();
        }
    }

    enum ProtectionType
    {
        AntiDebug,
        AntiILDasm,
        AntiTamper,
        Constants,
        ControlFlow,
        AntiDump,
        InvalidMetadata,
        ReferenceProxy,
        Resources,
        Rename
    }

    class Program
    {
        static private string outputPath = Directory.GetCurrentDirectory() + @"\Obfuscated\";
        static private string inputPath = Directory.GetCurrentDirectory() + @"\Unobfuscated\";
        static private string outputProject = Directory.GetCurrentDirectory() + @"\OutputConfuserProject.crproj";
        static private bool invokeObfuscator = true;
        static private string confuserPath = Directory.GetCurrentDirectory() + @"\..\..\..\ConfuserEx\Debug\Bin\Confuser.CLI.exe";

        static List<ModuleDescriptor> registeredApplications = new List<ModuleDescriptor>()
        {
            new ModuleDescriptor("SimpleConsoleApplication.exe")
            {
                protections = new List<ProtectionType>
                {
                    //ProtectionType.AntiDebug,
                    //ProtectionType.AntiDump,
                    ProtectionType.Constants,
                }
            }
        };

        static private readonly Dictionary<ProtectionType, string> mapProtectionType = new Dictionary<ProtectionType, string>()
        {
            {ProtectionType.AntiDebug, "anti debug" },
            {ProtectionType.AntiILDasm, "anti ildasm" },
            {ProtectionType.AntiTamper, "anti tamper" },
            {ProtectionType.Constants, "constants" },
            {ProtectionType.ControlFlow, "ctrl flow" },
            {ProtectionType.AntiDump,"anti dump" },
            {ProtectionType.InvalidMetadata,"invalid metadata" },
            {ProtectionType.ReferenceProxy,"ref proxy" },
            {ProtectionType.Resources,"resources" },
            {ProtectionType.Rename, "rename" },
        };

        static void Main(string[] args)
        {
            if (args.Length > 0)
                inputPath = args[0];
            if (args.Length > 1)
                outputPath = args[1];
            if (args.Length > 2)
                outputProject = args[2];
            if (args.Length > 4)
            {
                bool.TryParse(args[3], out invokeObfuscator);
                confuserPath = args[4] + "Confuser.CLI.exe";
            }

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
                    string protection = mapProtectionType[vv];
                    SettingItem<Protection> currentProtection = new SettingItem<Protection>(protection);
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
