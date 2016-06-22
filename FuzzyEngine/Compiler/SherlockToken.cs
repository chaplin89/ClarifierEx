using System;

namespace SherlockEngine
{
    internal class SherlockToken
    {
        SherlockTokenType tokenType;
        string value;
        int column;

        internal SherlockToken(string value, SherlockTokenType tokenType, int column)
        {
            this.value = value;
            this.tokenType = tokenType;
            this.column = column;
        }

        internal string Value
        {
            get
            {
                return value;
            }
        }

        internal SherlockTokenType TokenType
        {
            get
            {
                return tokenType;
            }
        }

        public int Column
        {
            get
            {
                return column;
            }
        }
        public override string ToString()
        {
            return string.Format("{0}: {1} {2}",Column,TokenType,Value );
        }
    }

    enum SherlockTokenType
    {
        Label,
        ParenthesesBegin,
        ParenthesesEnd,
        BinaryOperator,
        UnaryOperator,
        BracesStart,
        BracesEnd,
        BracketStart,
        BracketEnd,
        Variable,
        EqualSign,
        End,
        Key,
        Comma,
        Value,
        Null,
        JustStarted,
    }
}

