using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace FuzzyEngine
{
    public class ComparisonContext
    {
        int currentIndex;
        List<Instruction> instructionList;
        bool[] alreadyMatched;

        public List<Instruction> InstructionList
        {
            get
            {
                return instructionList;
            }

            set
            {
                instructionList = value;
            }
        }

        public bool[] AlreadyMatched
        {
            get
            {
                return alreadyMatched;
            }

            set
            {
                alreadyMatched = value;
            }
        }

        public int CurrentIndex
        {
            get
            {
                return currentIndex;
            }

            set
            {
                currentIndex = value;
            }
        }
    }
}