using Antlr4.Runtime;

namespace LimitCSolver;

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {

            /*
             *
             * Not in Use!
             *
             */

            var file = "test/1_1.c";

            var txt = File.ReadAllText(file);

            AntlrInputStream inputStream = new AntlrInputStream(txt);
            LimitCLexer LimitCLexer = new LimitCLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(LimitCLexer);
            LimitCParser LimitCParser = new LimitCParser(commonTokenStream);

            var LimitCContext = LimitCParser.prog(); // erstellen des Parsebaumes

            var functionDetector = new LimitCFunctionTreeBuilder(); // Funktions-Detektor
            functionDetector.Visit(LimitCContext); // erkennen von erkannten Funktionen

            var visitor = new LimitCVisitor(functionDetector.FunctionDefs, new Scope()); // main-Visitor, bekommt erkannte Funktionsdefinitionen und leeren global Scope
            visitor.Visit(LimitCContext); // abarbeitung starten

            //AntlrInputStream inputStream = new AntlrInputStream(txt);
            //LimitCLexer LimitCLexer = new LimitCLexer(inputStream);
            //CommonTokenStream commonTokenStream = new CommonTokenStream(LimitCLexer);
            //LimitCParser LimitCParser = new LimitCParser(commonTokenStream);
            //var LimitCContext = LimitCParser.prog();
            //var visitor = new LimitCVisitor();
            //visitor.Visit(LimitCContext);

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);                
        }
    }
}