using Antlr4.Runtime.Tree;

namespace LimitCSolver.SubTypes;

public class FunctionDef
{
    public FunctionDef(string name, string returnType, List<(string type, string name)> arguments, IParseTree parseTree)
    {
        Name = name;
        ReturnType = returnType;
        Arguments = arguments;
        ParseTree = parseTree;
    }

    public string Name { get; set; }
    public string ReturnType { get; set; }
    public List<(string type, string name)> Arguments { get; set; }
    public IParseTree ParseTree { get; set; }


}