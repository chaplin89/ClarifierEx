using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using Clarifier.Core;
using System.IO;

namespace Clarifier.Identification.Impl
{
    /// <summary>
    /// This manage the simplest form of protection, the one that 
    /// are based on injecting some methods inside the target assembly.
    /// So this class is capable of either found these methods (even if they
    /// are slightly modified) and remove all the refences inside the assembly.
    /// </summary>
    public abstract class BasicStaticProtection : IProtectionIdentificator, IProtectionRemover
    {
        protected string sourceModule = @".\Confuser.Runtime.dll";

        protected List<Tuple<string, string>> blacklist;
        protected Dictionary<Tuple<string, string>, MethodDef> blacklistMapInSource = null;
        protected Dictionary<Tuple<string, string>, List<MethodDef>> blacklistMapInDestination = null;
        protected double fuzzyThreshold = 0.70;

        public BasicStaticProtection()
        {
        }

        public virtual bool Initialize(IClarifierContext ctx)
        {
            if (!File.Exists(sourceModule))
                return false;
            blacklistMapInSource = new Dictionary<Tuple<string, string>, MethodDef>();

            try
            {
                ModuleDef confuserRuntime = AssemblyDef.Load(sourceModule).ManifestModule;
                blacklist.ForEach(x => blacklistMapInSource[x] = confuserRuntime.Find(x.Item1, true).FindMethod(x.Item2));
            }
            catch (Exception ex)
            {
                GC.KeepAlive(ex);
                return false;
            }
            return true;
        }

        public virtual bool PerformRemoval(IClarifierContext ctx)
        {
            bool returnValue = true;

            foreach (var identifiedMethods in blacklistMapInDestination)
            {
                foreach (var currentIdentifiedMethod in identifiedMethods.Value)
                {
                    foreach (var currentType in AllTypesHelper.Types(ctx.CurrentModule.Types))
                    {
                        foreach (var currentMethod in currentType.Methods)
                        {
                            if (currentMethod != currentIdentifiedMethod)
                                returnValue |= BodyModifier.RemoveRefence(currentMethod, currentIdentifiedMethod);
                        }
                    }
                    BodyModifier.NullifyMethod(currentIdentifiedMethod);
                }
            }
            return returnValue;
        }

        public virtual double PerformIdentification(IClarifierContext ctx)
        {
            double step = 1.0 / blacklist.Count;
            double returnValue = 0.0;

            blacklistMapInDestination = new Dictionary<Tuple<string, string>, List<MethodDef>>();

            foreach (var v in blacklistMapInSource)
            {
                var currentSimilarMethods = BodyComparison.GetSimilarMethods(ctx.CurrentModule, v.Value, true, fuzzyThreshold);
                if (currentSimilarMethods.Any())
                {
                    returnValue += step;
                    blacklistMapInDestination[v.Key] = currentSimilarMethods.ToList();
                }
            }
            return returnValue;
        }
    }
}
