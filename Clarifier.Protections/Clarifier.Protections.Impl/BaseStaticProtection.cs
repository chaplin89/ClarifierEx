using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using Clarifier.Core;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Clarifier.Identification.Impl
{
    public class PatternMatchingInfo
    {
        public string module;
        public string @namespace;
        public string name;
        public List<MethodDef> matchingMethods;
    };

    /// <summary>
    /// This manage the simplest form of protection, the one that 
    /// is based on injecting some methods inside the target assembly.
    /// This class is capable of either finding these methods (even if they
    /// are slightly modified) and either removing all the references 
    /// inside the assembly.
    /// </summary>
    public class BasicStaticProtection
    {
        private const string defaultSourceModule = @".\Confuser.Runtime.dll";
        private double fuzzyThreshold = 0.70;
        private List<PatternMatchingInfo> sourceMap = new List<PatternMatchingInfo>();
        private List<PatternMatchingInfo> destinationMap = new List<PatternMatchingInfo>();
        private bool typesLoadedCorrectly = false;

        public double FuzzyThreshold
        {
            set
            {
                fuzzyThreshold = value;
            }
        }

        public List<PatternMatchingInfo> SourceMap
        {
            get
            {
                return sourceMap;
            }
        }

        public List<PatternMatchingInfo> DestinationMap
        {
            get
            {
                return destinationMap;
            }
        }

        public BasicStaticProtection()
        {
        }

        public void AddPatternMatchingMethod(string namespaceToAdd, string nameToAdd, string moduleToAdd = defaultSourceModule)
        {
            sourceMap.Add(new PatternMatchingInfo() { @namespace = namespaceToAdd, name = nameToAdd, module = moduleToAdd });
        }

        /// <summary>
        /// This load the types from the source assembly.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>true if it manage to load all the types correctly; false otherwise</returns>
        public bool LoadTypes()
        {
            try
            {
                ModuleDef module = null;

                for (var i = 0; i < sourceMap.Count; ++i)
                {
                    if (module == null ||(module != null && module.Name != sourceMap[i].module))
                    {
                        if (!File.Exists(sourceMap[i].module))
                            return (typesLoadedCorrectly = false);
                        module = AssemblyDef.Load(sourceMap[i].module).ManifestModule;
                    }

                    MethodDef foundMethod = module.Find(sourceMap[i].@namespace, true).FindMethod(sourceMap[i].name);

                    sourceMap[i].matchingMethods = new List<MethodDef>() { foundMethod };
                }
            }
            catch (Exception ex)
            {
                GC.KeepAlive(ex);
                return (typesLoadedCorrectly = false);
            }
            return (typesLoadedCorrectly = true);
        }

        /// <summary>
        /// Iterate over all the methods of a given assembly and try to remove 
        /// all the references to the found methods.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public bool PerformRemoval(ModuleDef module)
        {
            bool returnValue = true;

            foreach (var identifiedMethods in destinationMap)
            {
                foreach (var currentIdentifiedMethod in identifiedMethods.matchingMethods)
                {
                    foreach (var currentType in AllTypesHelper.Types(module.Types))
                    {
                        foreach (var currentMethod in currentType.Methods)
                        {
                            if (!identifiedMethods.matchingMethods.Contains(currentMethod))
                                returnValue |= BodyModifier.RemoveRefence(currentMethod, currentIdentifiedMethod);
                        }
                    }
                    BodyModifier.NullifyMethod(currentIdentifiedMethod);
                }
            }
            return returnValue;
        }

        /// <summary>
        /// This takes the types loaded in LoadTypes and try to map them 
        /// in the target module using a fuzzy search.
        /// </summary>
        /// <param name="currentModule"></param>
        /// <returns></returns>
        public double MapSourceInDestination(ModuleDef currentModule)
        {
            Contract.Ensures(typesLoadedCorrectly);

            double step = 1.0 / sourceMap.Count;
            double returnValue = 0.0;

            foreach (var v in sourceMap)
            {
                Debug.Assert(v.matchingMethods.Count == 1);
                var currentSimilarMethods = BodyComparison.GetSimilarMethods(currentModule, v.matchingMethods[0], true, fuzzyThreshold);
                if (currentSimilarMethods.Any())
                {
                    returnValue += step;

                    PatternMatchingInfo pmToAdd = new PatternMatchingInfo()
                    {
                        @namespace = v.@namespace,
                        name = v.name,
                        module = v.module,
                        matchingMethods = currentSimilarMethods.ToList()
                    };
                    destinationMap.Add(pmToAdd);
                }
            }
            return returnValue;
        }
    }
}
