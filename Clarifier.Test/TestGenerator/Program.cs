using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confuser.Core.Project;
using System.Xml;
using Confuser.Core;
using System.IO;

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
        static List<ModuleDescriptor> registeredApplications = new List<ModuleDescriptor>()
        {
            new ModuleDescriptor("SimpleTestApplication.exe")
            {
                protections = new List<ProtectionType>
                {
                    ProtectionType.AntiDebug,
                    ProtectionType.AntiTamper,
                    ProtectionType.Constants
                }
            }
        };

        static private readonly string outputPath = @"..\Obfuscated\";
        static private readonly string inputPath = @".\";

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
            ConfuserProject module = new ConfuserProject()
            {
                OutputDirectory = Directory.GetCurrentDirectory() + outputPath,
                BaseDirectory = Directory.GetCurrentDirectory() + inputPath
            };

            foreach (var v in registeredApplications)
            {
                ProjectModule project = new ProjectModule() { Path = v.inputFileName };
                Rule moduleRule = new Rule();

                foreach (var vv in v.protections)
                {
                    string protection = mapProtectionType[vv];
                    SettingItem<Protection> currentProtection = new SettingItem<Protection>(protection);
                    moduleRule.Add(currentProtection);
                }

                project.Rules.Add(moduleRule);
                module.Add(project);
            }

            XmlDocument document = module.Save();

            using (XmlWriter xmlWrite = XmlWriter.Create(@".\OutputConfuserProject.crproj"))
                document.WriteContentTo(xmlWrite);
        }
    }
}
