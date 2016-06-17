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
        Func<ComparisonContext, FuzzyNode, bool> condition;
        public Func<ComparisonContext, FuzzyNode, ConditionOutcome> Condition
        {
            get;
            set;
        }

        protected object ValueOrChilds { get; set; }
        public NodeType Type { get; protected set; }

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
            if (Condition != null)
            {
                int matchNumber = 0;

                ConditionOutcome outcome;
                do
                {
                    outcome = Condition(ctx, this);
                    if (outcome != ConditionOutcome.Matched)
                        break;

                    ++matchNumber;
                } while (!MaxNumber.HasValue || matchNumber < MaxNumber);

                if (matchNumber < MinNumber)
                    return ConditionOutcome.NotMatched;
				else if (outcome == ConditionOutcome.Skip)
                    return ConditionOutcome.Skip;
				else
					return ConditionOutcome.Matched;
            }
            else if (Type == NodeType.Leaf)
            {
                int matchNumber = 0;
                do
                {
                    if (ctx.InstructionList[ctx.CurrentIndex++].OpCode != this.Value)
                        break;

                    ++ctx.CurrentIndex;
                    ++matchNumber;
                } while (!MaxNumber.HasValue || matchNumber < MaxNumber);

                if (matchNumber < MinNumber)
                {
                    ctx.CurrentIndex -= matchNumber;
                    return ConditionOutcome.NotMatched;
                }
            }
			else //if (Type == NodeType.Node)
            {
                int matchNumber = 0;
                do
                {
                    foreach (var v in Childs)
                    {
                        if (v.Test(ctx) == ConditionOutcome.Matched)
                            ++matchNumber;
                    }
                } while (!MaxNumber.HasValue || matchNumber < MaxNumber);

                if (matchNumber < MinNumber)
                    return ConditionOutcome.NotMatched;
            }

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
                return new FuzzyNode(retLeafValue);
            }

            FuzzyNode retNodeValue = new FuzzyNode();
            Childs.ForEach(x => retNodeValue.Childs.Add(x.Clone()));
            return retNodeValue;
        }
    }
}