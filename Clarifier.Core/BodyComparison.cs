using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Diagnostics.Contracts;

namespace Clarifier.Core
{
    static public class BodyComparison
    {
        public enum ComparisonMode
        {
            PreferFirst,
            PreferSecond,
            Mean
        }

        /// <summary>
        /// Simple comparison of two methods with fuzzy logic,
        /// used as fall back when a match is not found with boolean logic.
        /// This function should be capable of handling some (basic) situations in which:
        /// 1) The second method contains the same instruction of the first except from some
        ///     missing or modified instruction.
        /// 2) The second method contains repeated blocks taken from the first method.
        /// 3) The second method contains blocks taken from the first in shuffled order.
        /// <see cref="BooleanMethodsComparison"/>
        /// </summary>
        /// <param name="md1">First method to compare</param>
        /// <param name="md2">Second method to compare</param>
        /// <returns>True if the methods are matching, false otherwise</returns>
        static public bool FuzzyMethodsComparison(MethodDef md1, MethodDef md2, double threshold, ComparisonMode mode)
        {
            Contract.Ensures(md1.HasBody);

            if (!md2.HasBody)
                return false;

            bool[] matchedFirstMethod = new bool[md1.Body.Instructions.Count];
            bool[] matchedSecondMethod = new bool[md2.Body.Instructions.Count];

            Dictionary<OpCode, List<int>> mapSecondMethod = new Dictionary<OpCode, List<int>>();

            double thresholdToCommit = md1.Body.Instructions.Count * 0.15;

            // Step #1:
            // Reorganize all the instruction of the second method in order to create a dictionary opcode <==> positions
            // (where the instructions are found)
            for (var currentFirstIndex = 0; currentFirstIndex < md2.Body.Instructions.Count; ++currentFirstIndex)
            {
                if (!mapSecondMethod.ContainsKey(md2.Body.Instructions[currentFirstIndex].OpCode))
                    mapSecondMethod[md2.Body.Instructions[currentFirstIndex].OpCode] = new List<int>();

                mapSecondMethod[md2.Body.Instructions[currentFirstIndex].OpCode].Add(currentFirstIndex);
            }

            // Step #2:
            // For each instruction X of the first method, start a comparison between the instructions that follows X
            // and the instructions from the second method that follow an instruction with the same opcode of X (if any).
            //
            // In order to do this, the previous created dictionary is used.
            // From this step, the maximum range of instruction that match is taken.
            // When a range is chosen, if the length of this range is > of thresholdToCommit, the range is used to populate
            // a list of boolean (matchedFirstMethod and matchedSecondMethod).
            for (var currentFirstIndex = 0; currentFirstIndex < md1.Body.Instructions.Count; ++currentFirstIndex)
            {
                Instruction currentInstruction = md1.Body.Instructions[currentFirstIndex];
                List<Tuple<int, int>> rangeFirst = null;
                List<Tuple<int, int>> rangeSecond = null;
                int maxMatching = 0;

                if (!mapSecondMethod.ContainsKey(currentInstruction.OpCode))
                    continue;

                if (matchedFirstMethod[currentFirstIndex])
                    continue;

                foreach (var currentSecondIndex in mapSecondMethod[currentInstruction.OpCode])
                {
                    var firstMethodIndex = currentFirstIndex + 1;
                    var secondMethodIndex = currentSecondIndex + 1;
                    var currentMatching = 0;

                    while (firstMethodIndex < md1.Body.Instructions.Count &&
                           secondMethodIndex < md2.Body.Instructions.Count &&
                           md1.Body.Instructions[firstMethodIndex++].OpCode ==
                           md2.Body.Instructions[secondMethodIndex++].OpCode)
                    {
                        currentMatching++;
                    }

                    if (currentMatching > maxMatching)
                    {
                        rangeFirst = new List<Tuple<int, int>> { Tuple.Create(currentFirstIndex, firstMethodIndex) };
                        rangeSecond = new List<Tuple<int, int>> { Tuple.Create(currentSecondIndex, secondMethodIndex) };
                        maxMatching = currentMatching;
                    }
                    else if (maxMatching != 0 && currentMatching == maxMatching)
                    {
                        rangeFirst.Add(Tuple.Create(currentFirstIndex, firstMethodIndex));
                        rangeSecond.Add(Tuple.Create(currentSecondIndex, secondMethodIndex));
                    }
                }

                if (maxMatching > thresholdToCommit)
                {
                    foreach (var currentRange in rangeFirst)
                        foreach (var v in Enumerable.Range(currentRange.Item1, currentRange.Item2 - currentRange.Item1))
                            matchedFirstMethod[v] = true;
                    foreach (var currentRange in rangeSecond)
                        foreach (var v in Enumerable.Range(currentRange.Item1, currentRange.Item2 - currentRange.Item1))
                            matchedSecondMethod[v] = true;
                }
            }

            // Step #3: A value between 0,1 that indicate how much the two method are matching
            double computedThresholdFirst = 0.0;
            double computedThresholdSecond = 0.0;
            if (mode == ComparisonMode.PreferFirst || mode == ComparisonMode.Mean)
                computedThresholdFirst= (double)matchedFirstMethod.Where(x => x).Count() / md1.Body.Instructions.Count;
            if (mode == ComparisonMode.PreferSecond || mode == ComparisonMode.Mean)
                computedThresholdSecond = (double)matchedSecondMethod.Where(x => x).Count() / md2.Body.Instructions.Count;

            if (mode == ComparisonMode.PreferFirst)
                return computedThresholdFirst > threshold;
            else if (mode == ComparisonMode.PreferSecond)
                return computedThresholdSecond > threshold;
            else // (mode == ComparisonMode.Mean)
                return (computedThresholdFirst+ computedThresholdSecond)/2 > threshold;
        }

        /// <summary>
        /// Compare the instruction of two methods, operands are ignored.
        /// </summary>
        /// <param name="md1">First method</param>
        /// <param name="md2">Second method</param>
        /// <returns>True if the two methods have the same instruction.</returns>
        static public bool BooleanMethodsComparison(MethodDef md1, MethodDef md2)
        {
            if (md1.Body == null || md2.Body == null || md1.Body.Instructions.Count != md2.Body.Instructions.Count)
                return false;

            foreach (int i in Enumerable.Range(0, md1.Body.Instructions.Count))
            {
                if (md1.Body.Instructions[i].OpCode != md2.Body.Instructions[i].OpCode)
                    return false;

                if (md1.Body.Instructions[i].Operand.GetType() != md2.Body.Instructions[i].Operand.GetType())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Find a method inside a module.
        /// </summary>
        /// <param name="module">Module</param>
        /// <param name="methodToMatch">Method</param>
        /// <param name="fuzzy">Matching mode</param>
        /// <param name="threshold">Threshold of the fuzzy comparison (ignored if not fuzzy)</param>
        /// <returns>Returns all the method that matches.</returns>
        static public IEnumerable<MethodDef> GetSimilarMethods(ModuleDef module, MethodDef methodToMatch, bool fuzzy = false, double threshold = 0.0, ComparisonMode mode = ComparisonMode.PreferFirst)
        {
            foreach (var v in AllTypesHelper.Types(module.Types))
            {
                foreach (var vv in FindMethod(v, methodToMatch, fuzzy, threshold))
                    yield return vv;
            }
        }

        /// <summary>
        /// Find a method inside a type. Ignore nested type.
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="methodToMatch">The method</param>
        /// <param name="fuzzy">Matching mode</param>
        /// <param name="threshold">Threshold of the fuzzy comparison (ignored if not fuzzy)</param>
        /// <returns>Returns all the method that matches.</returns>
        static public IEnumerable<MethodDef> FindMethod(TypeDef type, MethodDef methodToMatch, bool fuzzy = false, double threshold = 0.0, ComparisonMode mode = ComparisonMode.PreferFirst)
        {
            foreach (var v in type.Methods)
            {
                if (fuzzy)
                    if (FuzzyMethodsComparison(methodToMatch, v, threshold, mode))
                        yield return v;
                else
                    if (BooleanMethodsComparison(v, methodToMatch))
                        yield return v;
            }
        }
    }
}
