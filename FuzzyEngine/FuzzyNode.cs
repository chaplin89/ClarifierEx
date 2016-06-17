using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FuzzyEngine{
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

        Func<ComparisonContext,FuzzyNode, bool> condition;
        public Func<ComparisonContext, FuzzyNode, ConditionOutcome> Condition
        {
            get;
            set;
        }

        public object ValueOrChilds { get; set; }
        public NodeType Type {
            get
            {
                if (ValueOrChilds is List<FuzzyNode>)
                    return NodeType.Node;
                return NodeType.Leaf;
            }
        }

        public OpCode Value
        {
            get
            {
                if (ValueOrChilds != null)
                    return (OpCode)ValueOrChilds;
                return OpCodes.Nop;
            }
        }
        public List<FuzzyNode> Childs
        {
            get
            {
                if (ValueOrChilds != null)
                    return (List<FuzzyNode>)ValueOrChilds;
                return null;
            }
        }
        public uint  MinNumber { get; set; }
        public uint? MaxNumber { get; set; }
        public string Name { get; set; }
        public FuzzyNode()
        {
            ValueOrChilds = new List<FuzzyNode>();
            MinNumber = 1;
            MaxNumber = null;
            Name = "";
        }

        public FuzzyNode(OpCode opcode)
        {
            ValueOrChilds = opcode;
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
                if (Childs.Where(x => x.Name == id).Any())
                    return Childs.Where(x => x.Name == id).Single();

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

            List<FuzzyNode> childs = ValueOrChilds as List<FuzzyNode>;

            if (childs != null)
            {
                foreach (var v in childs)
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
				while(matchNumber < this.MaxNumber)
                {
                    ConditionOutcome co = Condition(ctx, this);
                    if (co != ConditionOutcome.Matched)
                        return co;

                    ++ctx.CurrentIndex;
                    ++matchNumber;
                }
                if (matchNumber < this.MinNumber)
                    return ConditionOutcome.NotMatched;

                return ConditionOutcome.Matched;
            }

            if (Type == NodeType.Leaf)
            {
                if (ctx.InstructionList[ctx.CurrentIndex++].OpCode == this.Value)
					return ConditionOutcome.Matched;
                return ConditionOutcome.NotMatched;
            }

			if (Type == NodeType.Node)
            {
                int matchNumber = 0;
				while (matchNumber < this.MaxNumber)
                {
					foreach (var v in this.Childs)
                    {
                        ConditionOutcome co = v.Test(ctx);
                    }
                }
            }

            return ConditionOutcome.Matched;
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public FuzzyNode Clone()
        {
            if (Type == NodeType.Leaf)
            {
                OpCode retLeafValue = OpCodes.Nop;
                retLeafValue = Value;
                return new FuzzyNode(retLeafValue);
            }

            FuzzyNode retNodeValue = new FuzzyNode();
            foreach(var child in Childs)
            {
                retNodeValue.Childs.Add((FuzzyNode)child.Clone());
            }
            return retNodeValue;
        }
    }
}