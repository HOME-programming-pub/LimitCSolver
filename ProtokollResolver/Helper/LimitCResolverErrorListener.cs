using System.IO;
using System.Windows;
using Antlr4.Runtime;

namespace ProtokollResolver.Helper;

public class LimitCResolverErrorListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {

        MessageBox.Show(msg);
        
        base.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
    }
}