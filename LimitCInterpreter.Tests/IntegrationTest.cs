using Antlr4.Runtime;
using LimitCSolver.LimitCInterpreter.Parser;
using static System.Net.Mime.MediaTypeNames;

namespace LimitCSolver.LimitCInterpreter.Tests
{
    public class IntegrationTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ParseOKTest()
        {
            var file = ".\\TestData\\test1.c.test";
            var txt = File.ReadAllText(file);
            ParseTest(txt, 0);
        }

        [Test]
        public void ParseFailTest()
        {
            var file = ".\\TestData\\test2.c.test";
            var txt = File.ReadAllText(file);
            ParseTest(txt, 2);
        }

        private void ParseTest(string program, int expectedErrorCnt)
        {

            var inputStream = new AntlrInputStream(program);
            var lexer = new LimitCLexer(inputStream);
            var tkStream = new CommonTokenStream(lexer);
            var parser = LimitCParser.Instance(tkStream);

            var LimitCContext = parser.prog(); // erstellen des Parsebaumes

            Assert.That(parser.Errors.Count, Is.EqualTo(expectedErrorCnt));

            if(expectedErrorCnt > 0)
                return;

            var functionDetector = new LimitCFunctionTreeBuilder(); // Funktions-Detektor
            functionDetector.Visit(LimitCContext); // erkennen von erkannten Funktionen

            var functions = functionDetector.FunctionDefs;

            Assert.That(functions.Count, Is.EqualTo(1));
            Assert.That(functions.ElementAt(0).Key, Is.EqualTo("main"));
            Assert.That(functions.ElementAt(0).Value, Is.Not.Null);

            var mainFunction = functions.ElementAt(0).Value;
            Assert.That(mainFunction.ReturnType, Is.EqualTo(("int")));

            var visitor = new LimitCInterpreter(functionDetector.FunctionDefs, new Scope()); // main-Visitor, bekommt erkannte Funktionsdefinitionen und leeren global Scope
            var result = visitor.Visit(LimitCContext); // abarbeitung starten
            // Assert.IsNotNull(result);
        }
    }
}