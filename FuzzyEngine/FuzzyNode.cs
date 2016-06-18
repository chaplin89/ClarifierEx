using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FuzzyEngine
{
    public enum NodeType
    {
        Node,
        Leaf
    }
    public enum TestMode
    {
        InRange,
        MatchEverything
    }

    public enum ConditionOutcome
    {
        Matched,
        NotMatched,
        Skip
    }

    /// <summary>
    /// This represent either a single 
    /// instruction or a group of instructions
    /// </summary>
    public class FuzzyNode : ICloneable
    {
        public Func<ComparisonContext, FuzzyNode, ConditionOutcome> Condition
        {
            get;set;
        }
        protected object ValueOrChilds { get; set; }
        public NodeType Type { get; protected set; }
        public TestMode Mode { get; set; }
        public OpCode Value
        {
            get
            {
                if (Type == NodeType.Leaf)
                    return (OpCode)ValueOrChilds;
                throw new InvalidCastException();
            }
            set
            {
                ValueOrChilds = value;
                Type = NodeType.Leaf;
            }
        }
        public List<FuzzyNode> Childs
        {
            get
            {
                if (Type == NodeType.Node)
                    return (List<FuzzyNode>)ValueOrChilds;
                throw new InvalidCastException();
            }
            set
            {
                ValueOrChilds = value;
                Type = NodeType.Node;
            }
        }
        public uint MinNumber { get; set; }
        public uint? MaxNumber { get; set; }
        public string Name { get; set; }
        public FuzzyNode()
        {
            Childs = new List<FuzzyNode>();
            MinNumber = 1;
            MaxNumber = null;
            Name = "";
        }

        public FuzzyNode(IEnumerable<FuzzyNode> childs)
        {
            Childs = new List<FuzzyNode>();
            MinNumber = 1;
            MaxNumber = null;
            Name = "";

            foreach (var v in childs)
            {
                Childs.Add(v.Clone());
            }
        }

        public FuzzyNode(OpCode opcode)
        {
            Value = opcode;
            MinNumber = 1;
            MaxNumber = 1;
            Name = opcode.ToString();
        }

        public override string ToString()
        {
            return Name;
        }

        public FuzzyNode this[string id]
        {
            get
            {
                if (Type == NodeType.Leaf)
                    throw new InvalidOperationException();

                var nodes = Childs.Where(x => x.Name == id);

                if (nodes.Any())
                    return nodes.Single();

                var retVal = new FuzzyNode { Name = id };
                Childs.Add(retVal);
                return retVal;
            }
            set
            {
                var v = Childs.Where(x => x.Name == id).FirstOrDefault();
                if (v == null)
                {
                    v = new FuzzyNode { Name = id };
                    Childs.Add(v);
                }

                v.Childs.Add(value);
            }
        }

        public IEnumerable<FuzzyNode> WalkTree()
        {
            yield return this;

            if (Type == NodeType.Node)
            {
                foreach (var v in Childs)
                {
                    foreach (var vv in v.WalkTree())
                        yield return vv;
                }
            }
        }

        public ConditionOutcome Test(ComparisonContext ctx)
        {
            int totalMatch = 0;
            do
            {
                int iterationMatch = 0;

                if (ctx.CurrentIndex >= ctx.InstructionList.Count)
                    break;

                if (Type == NodeType.Leaf)
                {
                    if (ctx.InstructionList[ctx.CurrentIndex].OpCode == this.Value)
                        iterationMatch++;
                }
                else
                {
                    foreach (var v in Childs)
                    {
                        ConditionOutcome outcome = ConditionOutcome.NotMatched;
                        if (Condition != null)
                            outcome = Condition(ctx, v);
                        else
                            outcome = v.Test(ctx);

                        if (outcome == ConditionOutcome.Matched)
                        {
                            iterationMatch++;

                            if (Mode == TestMode.InRange)
                                break;
                        }
                        else if (Mode == TestMode.MatchEverything)
                        {
                            return ConditionOutcome.NotMatched;
                        }
                    }
                }

                if (iterationMatch == 0)
                    break;

                totalMatch += iterationMatch;
            } while ((Type == NodeType.Leaf || Mode == TestMode.InRange) &&
                     (!MaxNumber.HasValue   || totalMatch < MaxNumber));

            if (Type == NodeType.Leaf)
            {
                if (totalMatch < MinNumber)
                    return ConditionOutcome.NotMatched;
                return ConditionOutcome.Matched;
            }

            if ((Mode == TestMode.MatchEverything && totalMatch != Childs.Count) ||
                (Mode == TestMode.InRange && totalMatch < MinNumber))
                return ConditionOutcome.NotMatched;
            return ConditionOutcome.Matched;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public FuzzyNode Clone()
        {
            if (Type == NodeType.Leaf)
            {
                OpCode retLeafValue = Value;
                return new FuzzyNode(retLeafValue)
                {
                    Name = Name,
                    MinNumber = MinNumber,
                    MaxNumber = MaxNumber,
                    Condition = Condition
                };
            }

            FuzzyNode retNodeValue = new FuzzyNode()
            {
                Name = Name,
                MinNumber = MinNumber,
                MaxNumber = MaxNumber,
                Condition = Condition
            };

            Childs.ForEach(x => retNodeValue.Childs.Add(x.Clone()));
            return retNodeValue;
        }
    }
}