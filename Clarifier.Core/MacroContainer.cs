using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Clarifier.Core
{
    class GroupContainer
    {
        private InstructionGroup loadArgumentsInstructions;
        private InstructionGroup callInstructions;
        private InstructionGroup returnInstructions;

        public InstructionGroup CallInstructions
        {
            get
            {
                if (callInstructions == null)
                {
                    callInstructions = new InstructionGroup()
                    {
                        RecognizedInstructions = new List<OpCode>
                        {
                            OpCodes.Call,
                            OpCodes.Calli,
                            OpCodes.Callvirt,
                        },
                        HowMany = 1,
                        Name = "Call"
                    };
                }
                return callInstructions;
            }
        }

        public InstructionGroup ReturnInstructions
        {
            get
            {
                if (returnInstructions == null)
                {
                    returnInstructions = new InstructionGroup()
                    {
                        RecognizedInstructions = new List<OpCode>
                        {
                            OpCodes.Ret
                        },
                        HowMany = 1,
                        Name = "Return"
                    };
                }
                return returnInstructions;
            }
        }

        public InstructionGroup LoadArgumentsInstructions
        {
            get
            {
                if (loadArgumentsInstructions == null)
                {
                    loadArgumentsInstructions = new InstructionGroup()
                    {
                        RecognizedInstructions = new List<OpCode>
                        {
                            OpCodes.Ldarg,
                            OpCodes.Ldarga,
                            OpCodes.Ldarga_S,
                            OpCodes.Ldarg_0,
                            OpCodes.Ldarg_1,
                            OpCodes.Ldarg_2,
                            OpCodes.Ldarg_3,
                            OpCodes.Ldarg_S,
                        },
                        Name = "LoadArguments"
                    };
                }
                return loadArgumentsInstructions;
            }
        }
    }

    public class MacroContainer
    {
        GroupContainer groupContainer = new GroupContainer();
        private List<InstructionGroup> callMacro;

        public List<InstructionGroup> CallMacro
        {
            get
            {
                if (callMacro == null)
                {
                    callMacro = new List<InstructionGroup>
                    {
                        groupContainer.LoadArgumentsInstructions,
                        groupContainer.CallInstructions,
                        groupContainer.ReturnInstructions
                    };
                }
                return callMacro;
            }
        }
    }
}
