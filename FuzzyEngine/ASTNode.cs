
namespace FuzzyEngine
{
    public enum ASTOperation
    {
        Nop,
        And,
        Or,
        Not
    }

    internal class ASTNode
    {
        public ASTNode(ASTNode parent)
        {
            Parent = parent;
            first = null;
            second = null;
            operation = ASTOperation.Nop;
        }
        public ASTNode(string value, ASTNode parent) : this(parent)
        {
            Value = value;
        }

        ASTOperation operation;
        ASTNode first;
        ASTNode second;

        public ASTOperation Operation
        {
            get
            {
                return operation;
            }

            set
            {
                operation = value;
            }
        }

        public ASTNode First
        {
            get
            {
                return first;
            }

            set
            {
                first = value;
            }
        }

        public ASTNode Second
        {
            get
            {
                return second;
            }

            set
            {
                second = value;
            }
        }

        public string Value { get; internal set; }
        public ASTNode Parent { get; set; }
    }
}
