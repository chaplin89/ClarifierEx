using Stateless;
using System;
using System.Collections.Generic;

namespace SherlockEngine
{
    /// <summary>
    /// This is a stupid state machine that define a simple grammar, more or less defined by this BNF notation:
    /// 
    /// label ::= [_a-zA-Z0-9]+
    /// and ::= "&&"
    /// or ::= "||"
    /// not ::= "!"
    /// nullchars ::= " " | "\t"
    /// parenthesis_begin ::= "("
    /// parenthesis_end ::= ")"
    /// expression ::= 
    ///     | not, expression
    ///     | label
    ///     | parenthesis_begin, expression, parenthesis_end
    ///     | expression, and|or, expression
    ///
    /// It sucks, but for the needs of this language it's okay.
    /// Soon or later I'll think at something better.
    /// </summary>
    class SherlockParserState
    {
        StateMachine<SherlockTokenType, SherlockTokenType> stateMachine = new StateMachine<SherlockTokenType, SherlockTokenType>(SherlockTokenType.JustStarted);
        private bool inBraces = false;
        private bool inExpression = false;
        private Stack<int> level = new Stack<int>();
        public int CurrentColumn { get; set; }

        internal StateMachine<SherlockTokenType, SherlockTokenType> StateMachine
        {
            get
            {
                return stateMachine;
            }
        }

        public SherlockParserState()
        {
            stateMachine.Configure(SherlockTokenType.JustStarted)
                .Permit(SherlockTokenType.UnaryOperator, SherlockTokenType.UnaryOperator)
                .Permit(SherlockTokenType.Label, SherlockTokenType.Label)
                .Permit(SherlockTokenType.ParenthesesBegin, SherlockTokenType.ParenthesesBegin);

            stateMachine.Configure(SherlockTokenType.ParenthesesBegin)
                .OnEntry(() => { level.Push(CurrentColumn); inExpression = true; })
                .PermitReentry(SherlockTokenType.ParenthesesBegin)
                .Permit(SherlockTokenType.Label, SherlockTokenType.Label)
                .Permit(SherlockTokenType.UnaryOperator, SherlockTokenType.UnaryOperator);

            stateMachine.Configure(SherlockTokenType.Label)
                .Permit(SherlockTokenType.ParenthesesEnd, SherlockTokenType.ParenthesesEnd)
                .Permit(SherlockTokenType.BinaryOperator, SherlockTokenType.BinaryOperator)
                .Permit(SherlockTokenType.BracesStart, SherlockTokenType.BracesStart)
                .Permit(SherlockTokenType.End, SherlockTokenType.End);

            stateMachine.Configure(SherlockTokenType.UnaryOperator)
                .OnEntry(() => inExpression = true)
                .Permit(SherlockTokenType.Label, SherlockTokenType.Label)
                .Permit(SherlockTokenType.ParenthesesBegin, SherlockTokenType.ParenthesesBegin);

            stateMachine.Configure(SherlockTokenType.BinaryOperator)
                .OnEntry(() => inExpression = true)
                .Permit(SherlockTokenType.Label, SherlockTokenType.Label)
                .Permit(SherlockTokenType.ParenthesesBegin, SherlockTokenType.ParenthesesBegin)
                .Permit(SherlockTokenType.UnaryOperator, SherlockTokenType.UnaryOperator);

            stateMachine.Configure(SherlockTokenType.ParenthesesEnd)
                .OnEntry(() => { if (level.Count == 0) throw new InvalidOperationException(); level.Pop(); })
                .Permit(SherlockTokenType.UnaryOperator, SherlockTokenType.UnaryOperator)
                .Permit(SherlockTokenType.BinaryOperator, SherlockTokenType.BinaryOperator)
                .Permit(SherlockTokenType.BracketStart, SherlockTokenType.BracketStart)
                .Permit(SherlockTokenType.End, SherlockTokenType.End);

            stateMachine.Configure(SherlockTokenType.BracketStart)
                .Permit(SherlockTokenType.Key, SherlockTokenType.Key);
            stateMachine.Configure(SherlockTokenType.Key)
                .Permit(SherlockTokenType.EqualSign, SherlockTokenType.EqualSign);
            stateMachine.Configure(SherlockTokenType.EqualSign)
                .Permit(SherlockTokenType.Value, SherlockTokenType.Value);
            stateMachine.Configure(SherlockTokenType.Value)
                .Permit(SherlockTokenType.Comma, SherlockTokenType.Comma)
                .Permit(SherlockTokenType.BracketEnd, SherlockTokenType.BracketEnd);
            stateMachine.Configure(SherlockTokenType.Comma)
                .Permit(SherlockTokenType.Key, SherlockTokenType.Key);
            stateMachine.Configure(SherlockTokenType.BracketEnd)
                .Permit(SherlockTokenType.End, SherlockTokenType.End);

            stateMachine.Configure(SherlockTokenType.End)
                .OnEntry(() => inExpression = false)
                .Permit(SherlockTokenType.Label, SherlockTokenType.Label)
                .Permit(SherlockTokenType.ParenthesesBegin, SherlockTokenType.ParenthesesBegin)
                .Permit(SherlockTokenType.UnaryOperator, SherlockTokenType.UnaryOperator);

            stateMachine.Configure(SherlockTokenType.BracesStart)
                .OnEntry(() => { if (inBraces || inExpression) throw new InvalidOperationException(); inBraces = true; })
                .Permit(SherlockTokenType.Label, SherlockTokenType.Label)
                .Permit(SherlockTokenType.ParenthesesBegin, SherlockTokenType.ParenthesesBegin)
                .Permit(SherlockTokenType.UnaryOperator, SherlockTokenType.UnaryOperator);

            stateMachine.Configure(SherlockTokenType.BracesEnd)
                .OnEntry(() => { if (!inBraces) throw new InvalidOperationException(); inBraces = false; })
                .Permit(SherlockTokenType.Label, SherlockTokenType.Label)
                .Permit(SherlockTokenType.UnaryOperator, SherlockTokenType.UnaryOperator)
                .Permit(SherlockTokenType.ParenthesesBegin, SherlockTokenType.ParenthesesBegin);

            stateMachine.Configure(SherlockTokenType.Null)
                .Ignore(SherlockTokenType.Label)
                .Ignore(SherlockTokenType.ParenthesesBegin)
                .Ignore(SherlockTokenType.ParenthesesEnd)
                .Ignore(SherlockTokenType.BinaryOperator)
                .Ignore(SherlockTokenType.UnaryOperator)
                .Ignore(SherlockTokenType.BracesStart)
                .Ignore(SherlockTokenType.BracesEnd)
                .Ignore(SherlockTokenType.BracketStart)
                .Ignore(SherlockTokenType.BracketEnd)
                .Ignore(SherlockTokenType.Variable)
                .Ignore(SherlockTokenType.EqualSign)
                .Ignore(SherlockTokenType.End)
                .Ignore(SherlockTokenType.Key)
                .Ignore(SherlockTokenType.Comma)
                .Ignore(SherlockTokenType.Value)
                .Ignore(SherlockTokenType.JustStarted);
        }
    }
}
