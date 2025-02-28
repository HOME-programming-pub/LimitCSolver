using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LimitCSolver.LimitCInterpreter.Parser
{
    public partial class LimitCParser
    { 
        public List<Tuple<IToken, int, int, string, RecognitionException>> Errors { get; private set; }

        public class TestErrorListener : BaseErrorListener
        {
            public List<Tuple<IToken, int, int, string, RecognitionException>> Errors { get; set; }

            public TestErrorListener(List<Tuple<IToken, int, int, string, RecognitionException>> errors)
            {
                this.Errors = errors;
            }

            public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
            {
                var tuple = Tuple.Create(offendingSymbol, line, charPositionInLine, msg, e);
                Errors.Add(tuple);
            }
        }

        public static LimitCParser Instance(ITokenStream input)
        {
            var parser = new LimitCParser(input);
            parser.Errors = new List<Tuple<IToken, int, int, string, RecognitionException>>();
            var errorListener = new TestErrorListener(parser.Errors);
            parser.AddErrorListener(errorListener);
            return parser;
        }
        
       
    }
}
