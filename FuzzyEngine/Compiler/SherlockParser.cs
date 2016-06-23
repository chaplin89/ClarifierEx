using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SherlockEngine
{
    public class ParsingErrorException : Exception
    {
        public ParsingErrorException(string error) : base(error)
        {
        }
    }

    class SherlockParser
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        class SherlockHandlerAttribute : Attribute
        {
            public SherlockTokenType Type { get; private set; }
            public SherlockHandlerAttribute(SherlockTokenType type)
            {
                Type = type;
            }
        }

        SherlockLexer lexer = new SherlockLexer();
        SherlockParserState state = new SherlockParserState();

        Dictionary<SherlockTokenType, Func<ASTNode, SherlockToken, ASTNode>> mapTokenHandler;
        ASTNode toReturn;

        [SherlockHandler(SherlockTokenType.ParenthesesEnd)]
        [SherlockHandler(SherlockTokenType.ParenthesesBegin)]
        ASTNode HandleParentheses(SherlockToken token, ASTNode currentNode)
        {
            if (token.TokenType == SherlockTokenType.ParenthesesBegin)
            {
                currentNode.First = new ASTNode(currentNode);
                return currentNode.First;
            }
            return currentNode.Parent;
        }

        [SherlockHandler(SherlockTokenType.BinaryOperator)]
        ASTNode HandleBinary(SherlockToken token, ASTNode currentNode)
        {
            GC.KeepAlive(token);
            ASTNode tempNode = GetFreeNode(currentNode);

            if (token.Value == "&&")
                tempNode.Operation = ASTOperation.And;
            else if (token.Value == "||")
                tempNode.Operation = ASTOperation.Or;

            if (tempNode.Parent == null)
                toReturn = tempNode;

            return tempNode.Second;
        }

        [SherlockHandler(SherlockTokenType.UnaryOperator)]
        ASTNode HandleUnary(SherlockToken token, ASTNode currentNode)
        {
            GC.KeepAlive(token);
            currentNode.Operation = ASTOperation.Not;
            currentNode.First = new ASTNode(currentNode);
            return currentNode.First;
        }

        [SherlockHandler(SherlockTokenType.BracketEnd)]
        [SherlockHandler(SherlockTokenType.BracketStart)]
        ASTNode HandleBracket(SherlockToken token, ASTNode currentNode)
        {
            GC.KeepAlive(token);
            return currentNode;
        }

        [SherlockHandler(SherlockTokenType.BracesEnd)]
        [SherlockHandler(SherlockTokenType.BracesStart)]
        ASTNode HandleBraces(SherlockToken token, ASTNode currentNode)
        {
            GC.KeepAlive(token);
            return currentNode;
        }

        [SherlockHandler(SherlockTokenType.Value)]
        ASTNode HandleValues(SherlockToken token, ASTNode currentNode)
        {
            currentNode.Value = token.Value;
            return currentNode;
        }

        public SherlockParser()
        {
            //File.WriteAllText(".\\DotGraph",state.StateMachine.ToDotGraph());
            Type thisType = GetType();

            try
            {
                foreach (var method in thisType.GetMethods(BindingFlags.Instance & BindingFlags.NonPublic))
                {
                    foreach (var attribute in method.CustomAttributes)
                    {
                        if (attribute.AttributeType.Name == "SherlockHandlerAttribute")
                        {
                            SherlockTokenType type = (SherlockTokenType)attribute.NamedArguments.Single().TypedValue.Value;
                            Debug.Assert(!mapTokenHandler.ContainsKey(type));

                            mapTokenHandler[(SherlockTokenType)attribute.NamedArguments.Single().TypedValue.Value]
                                = (Func<ASTNode, SherlockToken, ASTNode>)method.CreateDelegate(typeof(Func<ASTNode, SherlockToken, ASTNode>), this);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Debug.Assert(false);
            }
        }

        public ASTNode Parse(string toParse)
        {
            ASTNode currentNode = new ASTNode(null);
            SherlockToken currentToken = null;

            try
            {
                lexer.PresetProgram(new StringReader(toParse));

                while ((currentToken = lexer.ReadNext()) != null)
                {
                    state.CurrentColumn = currentToken.Column;
                    if (!state.StateMachine.CanFire(currentToken.TokenType))
                        throw new InvalidOperationException();

                    state.StateMachine.Fire(currentToken.TokenType);

                    switch (currentToken.TokenType)
                    {
                        case SherlockTokenType.UnaryOperator:

                            break;
                        case SherlockTokenType.BinaryOperator:

                            break;
                        case SherlockTokenType.Label:
                            
                            break;
                    }
                }
            }
            catch (ParsingErrorException)
            {
                throw new ParsingErrorException(string.Format("Unexpected character '{0}' at column {1}.", currentToken.Value ?? "", currentToken.Column));
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