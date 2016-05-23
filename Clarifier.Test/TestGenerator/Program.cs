using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confuser.Core.Project;

namespace TestGenerator
{
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
            foreach(var v in registeredApplications)
            {
                ProjectModule module = new ProjectModule() { Path = v.Key };
                Rule moduleRule = new Rule();
                SettingItem<Confuser.Core.Protection> setting = new SettingItem<Confuser.Core.Protection>();

                moduleRule.Add(setting);
                module.Rules.Add(moduleRule);
            }
        }
    }
}
