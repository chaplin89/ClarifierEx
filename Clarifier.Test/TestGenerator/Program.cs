using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confuser.Core.Project;
using System.Xml;

namespace Clarifier.Test.TestGenerator
{
    class ModuleDescriptor
    {

    }

    class Program
    {
        static private Dictionary<string,List<string>> registeredApplications = new Dictionary<string, List<string>>
        {
            {
                "SimpleConsoleApplication.exe",  new List<string>
                {
                    ""
                }
            },
        };


        static void Main(string[] args)
        {
            ConfuserProject module = new ConfuserProject();
            foreach (var v in registeredApplications)
            {
                ProjectModule project = new ProjectModule();

                Rule moduleRule = new Rule();
                SettingItem<Confuser.Core.Protection> setting = new SettingItem<Confuser.Core.Protection>();

                moduleRule.Add(setting);
                project.Rules.Add(moduleRule);
                module.Add(project);
            }

            XmlDocument document = module.Save();
            XmlWriter xmlWrite = XmlWriter.Create(@".\OutputConfuserProject.crproj");
            document.WriteContentTo(xmlWrite);
        }
    }
}
