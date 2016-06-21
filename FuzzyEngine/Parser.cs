using System;
using System.Linq;
using System.Text;

namespace FuzzyEngine
{
    public class ParsingErrorException : Exception
    {
        string errorMsg;
        public ParsingErrorException(string error)
        {
            errorMsg = error;
        }
        public override string ToString()
        {
            return errorMsg;
        }
    }

    internal class Parser
    {
        ParsingRules sm = new ParsingRules();
        char[] allowedLabelChars = "_abcdefghijklmnopqrstuwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        int parsingIndex;

        public Parser()
        {
        }

        public ASTNode Parse(string toParse, ASTNode parent = null)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                ASTNode currentNode = new ASTNode(null);
                ASTNode toReturn = currentNode;
                ASTNode tempNode = null;

                for (; parsingIndex < toParse.Length; ++parsingIndex)
                {
                    switch (toParse[parsingIndex])
                    {
                        case '(':
                            sm.SetStatus(ParserStatus.ParenthesisBegin);
                            currentNode.First = new ASTNode(currentNode);
                            currentNode = currentNode.First;
                            break;
                        case ')':
                            sm.SetStatus(ParserStatus.ParenthesisEnd);
                            currentNode = currentNode.Parent;
                            break;
                        case '!':
                            sm.SetStatus(ParserStatus.Not);
                            currentNode.Operation = ASTOperation.Not;
                            currentNode.First = new ASTNode(currentNode);
                            currentNode = currentNode.First;
                            break;
                        case '&':
                            if (sm.CurrentStatus == ParserStatus.Ampersand)
                            {
                                sm.SetStatus(ParserStatus.SecondAmpersand);
                            }
                            else
                            {
                                sm.SetStatus(ParserStatus.Ampersand);
                                break;
                            }

                            tempNode = GetFreeNode(currentNode);
                            tempNode.Operation = ASTOperation.And;
                            currentNode = tempNode.Second;
                            if (tempNode.Parent == null)
                                toReturn = currentNode;
                            break;
                        case '|':
                            if (sm.CurrentStatus == ParserStatus.Pipe)
                            {
                                sm.SetStatus(ParserStatus.SecondPipe);
                            }
                            else
                            {
                                sm.SetStatus(ParserStatus.Pipe);
                                break;
                            }

                            tempNode = GetFreeNode(currentNode);
                            tempNode.Operation = ASTOperation.Or;
                            currentNode = tempNode.Second;

                            if (tempNode.Parent == null)
                                toReturn = currentNode;
                            break;
                        case ' ':
                            if (sm.CurrentStatus == ParserStatus.Label)
                                sm.SetStatus(ParserStatus.EndLabel);
                            else
                                sm.SetStatus(ParserStatus.Null);
                            break;
                        default:
                            if (!allowedLabelChars.Contains(toParse[parsingIndex]))
                            {
                                throw new ParsingErrorException(string.Format("Unexpected character {0} at column {1}", toParse[parsingIndex], parsingIndex));
                            }
                            sm.SetStatus(ParserStatus.Label);
                            currentNode.Value += toParse[parsingIndex];
                            break;
                    }
                }
                while (currentNode.Parent != null) { currentNode = currentNode.Parent; }
                return toReturn;
            }
            catch(InvalidTransitionException)
            {
                throw new ParsingErrorException(string.Format("Unexpected character '{0}' at column {1}.", toParse[parsingIndex], parsingIndex));
            }
        }

        private ASTNode GetFreeNode(ASTNode currentNode)
        {
            while (currentNode.Operation != ASTOperation.Nop || currentNode.Value != null)
            {
                if (currentNode.Parent == null)
                {
                    currentNode.Parent = new ASTNode(null);
                    currentNode.Parent.First = currentNode;
                    currentNode = currentNode.Parent;
                    break;
                }
                currentNode = currentNode.Parent;
            }
            currentNode.Second = new ASTNode(currentNode);
            return currentNode;
        }
    }
}