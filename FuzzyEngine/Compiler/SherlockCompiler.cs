
namespace SherlockEngine
{
    public class SherlockCompiler
    {
        SherlockLoader loader = new SherlockLoader();
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
            return ToSherlockNode(rootNode);
        }

        SherlockNode ToSherlockNode(ASTNode rootNode)
        {
            if (rootNode.Value != null)
            {
                return language[rootNode.Value];
            }

            else if (rootNode.Operation == ASTOperation.And)
            {
                SherlockNode first, second;
                first = ToSherlockNode(rootNode.First);
                second = ToSherlockNode(rootNode.Second);
                return first.Intersect(second);
            }
            else if (rootNode.Operation == ASTOperation.Not)
            {
                return language.Not(ToSherlockNode(rootNode.First));
            }
            else if (rootNode.Operation == ASTOperation.Or)
            {
                SherlockNode first, second;
                first = ToSherlockNode(rootNode.First);
                second = ToSherlockNode(rootNode.Second);
                return first.Union(second);
            }
            else if (rootNode.Operation == ASTOperation.Nop)
            {
                return ToSherlockNode(rootNode.First);
            }
            return null;
        }
    }
}