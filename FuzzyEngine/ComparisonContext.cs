using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace FuzzyEngine
{
    public class ComparisonContext
    {
        int currentIndex;
        IList<Instruction> instructionList;
        bool[] alreadyMatched;

        public IList<Instruction> InstructionList
        {
            get
            {
                return instructionList;
            }

            set
            {
                instructionList = value;
                AlreadyMatched = new bool[instructionList.Count];
                CurrentIndex = 0;
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