using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SherlockEngine
{
    public class ParsingErrorException : Exception
    {
        public ParsingErrorException(string error) : base(error)
        {
        }
    }

    internal class SherlockParser
    {
        SherlockLexer lexer = new SherlockLexer();
        SherlockParserState state = new SherlockParserState();

        public SherlockParser()
        {
        }

        public ASTNode Parse(string toParse)
        {
            ASTNode currentNode = new ASTNode(null);
            ASTNode toReturn = currentNode;
            ASTNode tempNode = null;
            SherlockToken currentToken = null;

            try
            {
                lexer.PresetProgram(new StringReader(toParse));

                while ((currentToken = lexer.ReadNext()) != null)
                {
                    state.CurrentColumn = currentToken.Column;
                    if (!state.StateMachine.CanFire(currentToken.TokenType))
                        throw new Exception();

                    try
                    {
                        state.StateMachine.Fire(currentToken.TokenType);
                    }
                    catch (InvalidOperationException)
                    {
                        throw new ParsingErrorException(string.Format("Unexpected character at position {0}", currentToken.Column));
                    }

                    switch (currentToken.TokenType)
                    {
                        case SherlockTokenType.ParenthesesBegin:
                            currentNode.First = new ASTNode(currentNode);
                            currentNode = currentNode.First;
                            break;
                        case SherlockTokenType.ParenthesesEnd:
                            currentNode = currentNode.Parent;
                            break;
                        case SherlockTokenType.UnaryOperator:
                            currentNode.Operation = ASTOperation.Not;
                            currentNode.First = new ASTNode(currentNode);
                            currentNode = currentNode.First;
                            break;
                        case SherlockTokenType.BinaryOperator:

                            tempNode = GetFreeNode(currentNode);

                            if (currentToken.Value == "&&")
                                tempNode.Operation = ASTOperation.And;
                            else if (currentToken.Value == "||")
                                tempNode.Operation = ASTOperation.Or;

                            currentNode = tempNode.Second;

                            if (tempNode.Parent == null)
                                toReturn = tempNode;
                            break;
                        case SherlockTokenType.Label:
                            currentNode.Value = currentToken.Value;
                            break;
                    }
                }
            }
            catch (ParsingErrorException)
            {
                throw new ParsingErrorException(string.Format("Unexpected character '{0}' at column {1}.", currentToken.Value?? "", currentToken.Column));
            }
            return toReturn;
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