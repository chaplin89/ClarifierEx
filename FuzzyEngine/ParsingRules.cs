using System;
using System.Collections.Generic;
using System.Linq;

namespace FuzzyEngine
{
    public class InvalidTransitionException : Exception
    {
        public InvalidTransitionException() { }
    }

    public enum ParserStatus
    {
        JustStarted,
        Label,
        EndLabel,
        Ampersand,
        SecondAmpersand,
        Pipe,
        SecondPipe,
        ParenthesisBegin,
        ParenthesisEnd,
        Not,
        Null
    }

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
    public class ParsingRules
    {
        ParserStatus currentStatus = ParserStatus.JustStarted;
        Dictionary<ParserStatus, ParserStatus[]> allowedTransitions = new Dictionary<ParserStatus, ParserStatus[]>();

        public ParsingRules()
        {
            allowedTransitions[ParserStatus.JustStarted] = new ParserStatus[]
            {
                ParserStatus.Label,
                ParserStatus.Not,
                ParserStatus.ParenthesisBegin,
                ParserStatus.Null
            };
            allowedTransitions[ParserStatus.Ampersand] = new ParserStatus[]
            {
                ParserStatus.SecondAmpersand
            };
            allowedTransitions[ParserStatus.Pipe] = new ParserStatus[]
            {
                ParserStatus.SecondPipe
            };
            allowedTransitions[ParserStatus.SecondPipe] =
            allowedTransitions[ParserStatus.SecondAmpersand] = new ParserStatus[]
            {
                ParserStatus.Label,
                ParserStatus.ParenthesisBegin,
                ParserStatus.Not,
                ParserStatus.Null
            };
            allowedTransitions[ParserStatus.Null] =
            allowedTransitions[ParserStatus.ParenthesisBegin] = new ParserStatus[]
            {
                ParserStatus.Label,
                ParserStatus.Not,
                ParserStatus.ParenthesisBegin,
                ParserStatus.Null
            };
            allowedTransitions[ParserStatus.ParenthesisEnd] = new ParserStatus[]
            {
                ParserStatus.ParenthesisEnd,
                ParserStatus.Pipe,
                ParserStatus.Ampersand,
                ParserStatus.Not,
                ParserStatus.Null
            };
            allowedTransitions[ParserStatus.Not] = new ParserStatus[]
            {
                ParserStatus.Label,
                ParserStatus.ParenthesisBegin,
                ParserStatus.Null
            };
            allowedTransitions[ParserStatus.Label] = new ParserStatus[]
            {
                ParserStatus.Label,
                ParserStatus.EndLabel,
                ParserStatus.Ampersand,
                ParserStatus.Pipe,
                ParserStatus.ParenthesisEnd
            };
            allowedTransitions[ParserStatus.EndLabel] = new ParserStatus[]
            {
                ParserStatus.Ampersand,
                ParserStatus.Pipe,
                ParserStatus.ParenthesisEnd
            };
        }

        public ParserStatus CurrentStatus
        {
            get
            {
                return currentStatus;
            }
        }

        /// <summary>
        /// Try to set a status, throw exception if transition is not permitted by rules.
        /// </summary>
        /// <param name="status">Target status</param>
        public void SetStatus(ParserStatus status)
        {
            if (!allowedTransitions.ContainsKey(status))
                throw new InvalidTransitionException();

            if (!allowedTransitions[CurrentStatus].Contains(status))
                throw new InvalidTransitionException();

            currentStatus = status;
        }
    }
}
