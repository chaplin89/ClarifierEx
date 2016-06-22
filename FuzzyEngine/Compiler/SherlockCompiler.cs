
namespace SherlockEngine
{
    public class SherlockCompiler
    {
        Loader loader = new Loader();
        SherlockParser parser = new SherlockParser();
        SherlockNode language = null;

        void InitCompiler()
        {
            language = loader.LoadLanguage();
        }
        
        public SherlockNode Compile(string toCompile)
        {
            if (language == null)
                InitCompiler();
            ASTNode rootNode = parser.Parse(toCompile);
            return MakeFuzzyNode(rootNode);
        }

        SherlockNode MakeFuzzyNode(ASTNode rootNode)
        {
            if (rootNode.Value != null)
            {
                return language[rootNode.Value];
            }
            else if (rootNode.Operation == ASTOperation.And)
            {
                SherlockNode first, second;
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
                SherlockNode first, second;
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