using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SherlockEngine
{
    public enum NodeType
    {
        Node,
        Leaf
    }
    public enum TestMode
    {
        InRange,
        MatchEverything,
        Fuzzy
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
    public class SherlockNode : ICloneable, IEquatable<SherlockNode>
    {
        public Func<ComparisonContext, SherlockNode, ConditionOutcome> Condition
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

        public SherlockNode Not(SherlockNode second)
        {
            SherlockNode toReturn = new SherlockNode();
            foreach (var firstChilds in GetLeafs())
            {
                bool found = false;
                foreach (var secondChilds in second.GetLeafs())
                {
                    if (firstChilds.Equals(secondChilds))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    toReturn.Childs.Add(firstChilds.Clone());
            }
            return toReturn;
        }

        public SherlockNode Union(SherlockNode second)
        {
            SherlockNode toReturn = Clone();
            foreach (var v in second.GetLeafs())
            {
                bool found = false;
                foreach (var vv in GetLeafs())
                {
                    if (v.Equals(vv))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    toReturn.Childs.Add(v.Clone());
            }
            return toReturn;
        }

        public SherlockNode Intersect(SherlockNode second)
        {
            SherlockNode toReturn = new SherlockNode();
            foreach (var firstChilds in GetLeafs())
            {
                foreach (var secondChilds in second.GetLeafs())
                {
                    if (firstChilds.Equals(secondChilds))
                    {
                        toReturn.Childs.Add(firstChilds.Clone());
                        break;
                    }
                }
            }
            return toReturn;
        }

        public List<SherlockNode> Childs
        {
            get
            {
                if (Type == NodeType.Node)
                    return (List<SherlockNode>)ValueOrChilds;
                throw new InvalidCastException();
            }
            set
            {
                ValueOrChilds = value;
                Type = NodeType.Node;
            }
        }

        bool [] Matched { get; set; }
        public uint MinNumber { get; set; }
        public uint? MaxNumber { get; set; }
        public string Name { get; set; }
        public SherlockNode()
        {
            Childs = new List<SherlockNode>();
            MinNumber = 1;
            MaxNumber = null;
            Name = "";
        }

        public SherlockNode(IEnumerable<SherlockNode> childs)
        {
            Childs = new List<SherlockNode>();
            MinNumber = 1;
            MaxNumber = null;
            Name = "";

            foreach (var v in childs)
            {
                Childs.Add(v.Clone());
            }
        }

        public SherlockNode(OpCode opcode)
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



        public SherlockNode this[string id]
        {
            get
            {
                if (Type == NodeType.Leaf)
                    throw new InvalidOperationException();

                var nodes = Childs.Where(x => x.Name == id);

                if (nodes.Any())
                    return nodes.Single();

                var retVal = new SherlockNode { Name = id };
                Childs.Add(retVal);
                return retVal;
            }
            set
            {
                var v = Childs.Where(x => x.Name == id).FirstOrDefault();
                if (v == null)
                {
                    v = new SherlockNode { Name = id };
                    Childs.Add(v);
                }

                v.Childs.Add(value);
            }
        }

        public IEnumerable<SherlockNode> WalkTree()
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

        public IEnumerable<SherlockNode> GetLeafs()
        {
            if(Type == NodeType.Leaf)
            {
                yield return this;
                yield break;
            }

            if (Type == NodeType.Node)
            {
                foreach (var v in Childs)
                {
                    foreach (var vv in v.GetLeafs())
                        yield return vv;
                }
            }
        }

        public double FuzzyTest(ComparisonContext ctx)
        {
            int totalMatch = 0;
            int lastChildMatchedIndex = 0;
            int savedContextIndex = 0;
            bool contextChangeInProgress = false;

            bool[] currentMatched = new bool[ctx.InstructionList.Count];
            bool[] lastCurrentMatched = ctx.AlreadyMatched;
            ctx.AlreadyMatched = currentMatched;

            do
            {
                int iterationMatch = 0;

                if (ctx.CurrentIndex >= ctx.InstructionList.Count)
                    break;

                if (Type == NodeType.Leaf)
                {
                    if (ctx.InstructionList[ctx.CurrentIndex].OpCode == this.Value)
                    {
                        iterationMatch++;
                        ctx.AlreadyMatched[ctx.CurrentIndex] = true;
                    }
                }
                else // if (Type == NodeType.Node)
                {
                    bool contextChanged = false;
                    for (; lastChildMatchedIndex < Childs.Count; lastChildMatchedIndex++)
                    {
                        var v = Childs[lastChildMatchedIndex];

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
                            if (!contextChangeInProgress)
                            {
                                savedContextIndex = ctx.CurrentIndex;
                                contextChangeInProgress = true;
                            }
                            if (ctx.CurrentIndex+1 >= ctx.InstructionList.Count)
                            {
                                ctx.CurrentIndex = savedContextIndex;
                                lastChildMatchedIndex++;
                                break;
                            }

                            ctx.CurrentIndex++;
                            contextChanged = true;
                            break;
                        }
                    }

                    if (contextChanged)
                    {
                        contextChanged = false;
                        continue;
                    }
                }

                if (iterationMatch == 0)
                    break;

                totalMatch += iterationMatch;
            } while ((Type == NodeType.Leaf || Mode == TestMode.InRange) &&
                     (!MaxNumber.HasValue || totalMatch < MaxNumber));

            // Commit eventually current matched position in the master array



            // Reason for returning NotMatched:
            // 1. If this is a Leaf:
            //    1.1: Matched number less than MinNumber
            // 2. If this is a Node:
            //    2.1: Comparison mode is InRange and matching number is outside this range
            //    2.2: Comparison mode is MatchEverything and not everything is matched

            if (((Type == NodeType.Leaf || Mode == TestMode.InRange) && totalMatch < MinNumber) ||
                (Mode == TestMode.MatchEverything && totalMatch != Childs.Count))
            {
                //return ConditionOutcome.NotMatched;
            }
            //return ConditionOutcome.Matched;
            return 0;
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
                else // if (Type == NodeType.Node)
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

            // Reason for returning NotMatched:
            // 1. If this is a Leaf:
            //    1.1: Matched number less than MinNumber
            // 2. If this is a Node:
            //    2.1: Comparison mode is InRange and matching number is outside this range
            //    2.2: Comparison mode is MatchEverything and not everything is matched

            if (((Type == NodeType.Leaf || Mode == TestMode.InRange) && totalMatch < MinNumber) ||
                (Mode == TestMode.MatchEverything && totalMatch != Childs.Count))
                return ConditionOutcome.NotMatched;

            return ConditionOutcome.Matched;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public SherlockNode Clone()
        {
            if (Type == NodeType.Leaf)
            {
                return new SherlockNode(Value)
                {
                    Name = Name,
                    MinNumber = MinNumber,
                    MaxNumber = MaxNumber,
                    Condition = Condition
                };
            }

            SherlockNode retNodeValue = new SherlockNode()
            {
                Name = Name,
                MinNumber = MinNumber,
                MaxNumber = MaxNumber,
                Condition = Condition
            };

            Childs.ForEach(x => retNodeValue.Childs.Add(x.Clone()));
            return retNodeValue;
        }

        public bool Equals(SherlockNode other)
        {
            if (!CompareProperties(other))
                return false;

            if (Type == NodeType.Leaf)
                return true;

            if (Childs.Count != other.Childs.Count)
                return false;

            for(int i=0; i<Childs.Count; ++i)
            {
                if (!other.Childs[i].Equals(Childs[i]))
                    return false;
            }
            return true;
        }

        public bool CompareProperties(SherlockNode other)
        {
            if (Type != other.Type)
                return false;
            if (this.Mode != other.Mode)
                return false;
            if (this.MinNumber != other.MinNumber)
                return false;
            if (this.MaxNumber != other.MaxNumber)
                return false;
            if (this.Value != other.Value)
                return false;
            return true;
        }
    }
}