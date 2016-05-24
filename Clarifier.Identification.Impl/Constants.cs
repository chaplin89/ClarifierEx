using Clarifier.Core;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Clarifier.Identification.Impl
{
    class Constants : BasicStaticProtection
    {
        public Constants()
        {
            blacklist = new List<Tuple<string, string>>
            {
                Tuple.Create("Confuser.Runtime.Constant","Get"),
                Tuple.Create("Confuser.Runtime.Constant","Initialize")
            };
        }

        public override bool Initialize(ClarifierContext ctx)
        {
            return base.Initialize(ctx);
        }

        public override bool PerformRemoval(ClarifierContext ctx)
        {
            byte[] newAssembly = ClarifierInjectHelper.GetBrandNewAssemblyFromType(ctx.CurrentModule.GlobalType);

            if (Debugger.IsAttached)
            {
                File.WriteAllBytes(@".\TestAssembly.dll", newAssembly);
            }

            Assembly asm = Assembly.Load(newAssembly);

            Type dummyType = asm.ManifestModule.GetType("DummyNamespace.DummyType");
            object dummyInstance = Activator.CreateInstance(dummyType);

            Dictionary<string, MethodInfo> mapMethodsToName = new Dictionary<string, MethodInfo>();

            foreach(var v in blacklistMapInDestination)
            {
                string currentName = string.Format("DummyNamespace.DummyType.{0}");
                mapMethodsToName[currentName] = dummyType.GetMethod(currentName);
            }

            foreach (var identifiedMethods in blacklistMapInDestination)
            {
                foreach (var currentIdentifiedMethod in identifiedMethods.Value)
                {
                    foreach (var currentType in AllTypesHelper.Types(ctx.CurrentModule.Types))
                    {
                        foreach (var currentMethod in currentType.Methods)
                        {
                        }
                    }
                }
            }

            foreach (var v in mapMethodsToName)
            {
                if (v.Value.IsGenericMethod)
                {
                }

                try
                {
                    object wtfff = mapMethodsToName[v.Key].MakeGenericMethod(typeof(string)).Invoke(null, new object[] { 226098525u });
                }
                catch
                {

                }
            }
            return true;
//             BodyModifier.FindAndReplaceWithResult(toReplace, targetModule, mapMethodsToName, dummyInstance);
//             foreach (var v in identifiedMethods)
//             {
//                 foreach (var currentType in AllTypesHelper.Types(ctx.CurrentModule.Types))
//                 {
//                     foreach (var currentMethod in currentType.Methods)
//                     {
//                         if (v != currentMethod)
//                         {
// 
//                         }
//                     }
//                 }
//             }
//             return true;
        }

        public override double PerformIdentification(ClarifierContext ctx)
        {
            return base.PerformIdentification(ctx);
        }
    }
}
