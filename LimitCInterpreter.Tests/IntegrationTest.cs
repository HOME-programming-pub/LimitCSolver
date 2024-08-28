using static System.Formats.Asn1.AsnWriter;
using Antlr4.Runtime;
using Model;

namespace LimitCInterpreter.Tests
{
    public class IntegrationTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {

            var file = ".\\TestData\\test1.c.test";

            var txt = File.ReadAllText(file);

            AntlrInputStream inputStream = new AntlrInputStream(txt);
            LimitCLexer LimitCLexer = new LimitCLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(LimitCLexer);
            LimitCParser LimitCParser = new LimitCParser(commonTokenStream);

            var LimitCContext = LimitCParser.prog(); // erstellen des Parsebaumes

            var functionDetector = new LimitCFunctionTreeBuilder(); // Funktions-Detektor
            functionDetector.Visit(LimitCContext); // erkennen von erkannten Funktionen

            var functions = functionDetector.FunctionDefs;

            Assert.That(functions.Count, Is.EqualTo(1));
            Assert.That(functions.ElementAt(0).Key, Is.EqualTo("main"));
            Assert.That(functions.ElementAt(0).Value, Is.Not.Null);

            var mainFunction = functions.ElementAt(0).Value;
            Assert.That(mainFunction.ReturnType, Is.EqualTo(("int")));

            var visitor = new Model.LimitCInterpreter(functionDetector.FunctionDefs, new Model.Scope()); // main-Visitor, bekommt erkannte Funktionsdefinitionen und leeren global Scope
            var result = visitor.Visit(LimitCContext); // abarbeitung starten

            // Assert.IsNotNull(result);

        }
    }
}