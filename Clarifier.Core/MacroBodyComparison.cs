using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Clarifier.Core
{
    public class InstructionGroup
    {
        List<OpCode> recognizedInstructions = null;
        int? howMany = null;
        bool isOptional = false;
        List<int> foundInstructions = new List<int>();

        public override string ToString()
        {
            return Name;
        }

        public string Name { get; set; }
        public int? HowMany
        {
            get { return howMany; }
            set { howMany = value; }
        }
        public List<OpCode> RecognizedInstructions
        {
            get { return recognizedInstructions; }
            set { recognizedInstructions = value; }
        }
        public bool IsOptional
        {
            get { return isOptional; }
            set { isOptional = value; }
        }
        public List<int> FoundInstructions
        {
            get { return foundInstructions; }
            set { foundInstructions = value; }
        }
    }

    public class MacroBodyComparison
    {
        List<InstructionGroup> instructionGroups = new List<InstructionGroup>();

        public List<InstructionGroup> InstructionGroups
        {
            get { return instructionGroups; }
            set { instructionGroups = value; }
        }

        public bool PerformComparison(MethodDef method, bool preserveOrder = true, bool allowOtherInstruction = false)
        {
            int groupIndex = 0;
            for (var i = 0; i < method.GetInstructions().Count; ++i)
            {
                var instruction = method.Body.Instructions[i];
                if (InstructionGroups.Count <= groupIndex && !allowOtherInstruction)
                    return false;
                else if (InstructionGroups.Count <= groupIndex)
                    break;

                bool hasLimit = InstructionGroups[groupIndex].HowMany.HasValue;
                int limit = InstructionGroups[groupIndex].HowMany.GetValueOrDefault();

                //At this moment this algorithm doesn't manage:
                // unrecognized -> optional -> non optional
                //But only:
                // optional -> unrecognized -> non optional
                //(Not planning to add this use case, though)
                bool isRecognized = InstructionGroups[groupIndex].RecognizedInstructions.Contains(instruction.OpCode);

                while (!isRecognized && ((InstructionGroups[groupIndex].IsOptional) ||
                                         (hasLimit && InstructionGroups[groupIndex].FoundInstructions.Count == limit) || 
                                         (!hasLimit && InstructionGroups[groupIndex].FoundInstructions.Count>0)))
                {
                    //If you're here:
                    // Either you're skipping a not found, optional instruction,
                    // or you're skipping a group of instruction you've already matched in a 
                    // previous iteration.
                    groupIndex++;
                    isRecognized = InstructionGroups[groupIndex].RecognizedInstructions.Contains(instruction.OpCode);
                }

                //If you're here:
                // Not optional, found
                // Not optional, not found
                // Optional, found
                if (isRecognized)
                    InstructionGroups[groupIndex].FoundInstructions.Add(i);
                else if (allowOtherInstruction)
                    continue;
                else if (!InstructionGroups[groupIndex].IsOptional)
                    return false;
            }

            foreach (var group in instructionGroups)
            {
                if (!group.HowMany.HasValue)
                {
                    if (!group.IsOptional && group.FoundInstructions.Count == 0)
                        return false;
                }
                else
                {
                    if (!group.IsOptional && group.FoundInstructions.Count!=group.HowMany.Value)
                        return false;
                }
            }

            return true;
        }
    }
}
