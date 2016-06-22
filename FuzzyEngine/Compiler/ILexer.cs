using System;

namespace SherlockEngine
{
    internal interface ILexer<TokenType>
    {
        bool CanRead { get; }
        TokenType Peek();
        TokenType ReadNext(bool moveIndex = false);
    }
}