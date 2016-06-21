namespace FuzzyEngine
{
    public class Compiler
    {
        Loader loader = new Loader();
        Parser parser = new Parser();
        FuzzyNode language = null;

        void InitCompiler()
        {
            language = loader.LoadLanguage();
        }
        
        public FuzzyNode Compile(string toCompile)
        {
            if (language == null)
                InitCompiler();
            ASTNode rootNode = parser.Parse(toCompile);
            return MakeFuzzyNode(rootNode);
        }

        FuzzyNode MakeFuzzyNode(ASTNode rootNode)
        {
            if (rootNode.Value != null)
            {
                return language[rootNode.Value];
            }
            else if (rootNode.Operation == ASTOperation.And)
            {
                FuzzyNode first, second;
                first = MakeFuzzyNode(rootNode.First);
                second = MakeFuzzyNode(rootNode.Second);
                return first.Intersect(second);
            }
            else if (rootNode.Operation == ASTOperation.Not)
            {
                return language.Not(MakeFuzzyNode(rootNode.First));
            }
            else if (rootNode.Operation == ASTOperation.Or)
            {
                FuzzyNode first, second;
                first = MakeFuzzyNode(rootNode.First);
                second = MakeFuzzyNode(rootNode.Second);
                return first.Union(second);
            }
            else if (rootNode.Operation == ASTOperation.Nop)
            {
                return MakeFuzzyNode(rootNode.First);
            }
            return null;
        }
    }
}
