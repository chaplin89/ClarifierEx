namespace SherlockEngine
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
            First = null;
            Second = null;
            Operation = ASTOperation.Nop;
        }

        public ASTNode(string value, ASTNode parent) : this(parent)
        {
            Value = value;
        }

        public ASTOperation Operation { get; set; }
        public ASTNode First { get; set; }
        public ASTNode Second { get; set; }
        public string Value { get; set; }
        public ASTNode Parent { get; set; }
    }
}
