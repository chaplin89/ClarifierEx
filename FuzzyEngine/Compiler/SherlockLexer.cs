using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SherlockEngine
{
    class SherlockLexer : ILexer<SherlockToken>
    {
        TextReader toCompile;
        bool canRead;
        string cachedLine;
        int currentColumn;

        Dictionary<Regex, SherlockTokenType> mapCharType = new Dictionary<Regex, SherlockTokenType>
        {
            {new Regex(@"^\{"), SherlockTokenType.BracesEnd},
            {new Regex(@"^\}"), SherlockTokenType.BracesEnd},
            {new Regex(@"^\["), SherlockTokenType.BracesStart },
            {new Regex(@"^\]"), SherlockTokenType.BracketEnd },
            {new Regex(@"^\("), SherlockTokenType.ParenthesesBegin },
            {new Regex(@"^\)"), SherlockTokenType.ParenthesesEnd },
            {new Regex(@"^!"), SherlockTokenType.UnaryOperator },
            {new Regex(@"^&&"), SherlockTokenType.BinaryOperator },
            {new Regex(@"^\|\|"), SherlockTokenType.BinaryOperator },
            {new Regex(@"^="), SherlockTokenType.EqualSign },
            {new Regex(@"^;"), SherlockTokenType.End },
            {new Regex(@"^[a-zA-Z0-9_]+"), SherlockTokenType.Label },
            {new Regex(@"^\$[a-zA-Z0-9_]+"), SherlockTokenType.Variable },
            {new Regex(@"[\t\s(\r\n?|\n)]+"), SherlockTokenType.Null }
        };

        public bool CanRead
        {
            get
            {
                return canRead;
            }
        }

        public void PresetProgram(TextReader toCompile)
        {
            this.toCompile = toCompile;
        }

        private void CacheNextLine()
        {
            StringBuilder sb = new StringBuilder();
            int readedChar;
            while ((readedChar = toCompile.Read()) != -1)
            {
                sb.Append((char)readedChar);
                if (readedChar == ';')
                    break;
            }
            cachedLine = sb.ToString();
        }

        public SherlockToken Peek()
        {
            return ReadNext(false);
        }
        public SherlockToken ReadNext(bool moveIndex = true)
        {
            if (cachedLine == null)
                CacheNextLine();

            if (cachedLine.Length <= currentColumn)
            {
                cachedLine = null;
                return null;
            }

            int startPosition = currentColumn;
            bool skip;

            do
            {
                string currentPart = cachedLine.Substring(currentColumn);
                skip = false;
                foreach (var v in mapCharType)
                {
                    Match vv = v.Key.Match(currentPart);
                    if (vv.Success)
                    {
                        System.Diagnostics.Debug.Assert(vv.Index == 0);
                        if (v.Value == SherlockTokenType.Null)
                        {
                            if (moveIndex)
                                currentColumn += vv.Length;
                            skip = true;
                            break;
                        }

                        if (vv.Index != 0)
                            continue;
                        if (moveIndex)
                            currentColumn += vv.Length;

                        return new SherlockToken(vv.Value, v.Value, startPosition);
                    }
                }
            } while (skip);

            return null;
        }
    }


}
