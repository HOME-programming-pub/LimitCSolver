using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using LimitCSolver.LimitCInterpreter.Memory;
using LimitCSolver.LimitCInterpreter.SubTypes;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LimitCSolver.LimitCInterpreter;

public partial class LimitCInterpreter : LimitCBaseVisitor<object?>
{

    public Dictionary<string, FunctionDef> FunctionDefs;
    //private int _memoryLastPos = 0;
    public MemoryStorage MemoryStorage;

    private readonly Stack<Scope> _scopes = new();
    private readonly Scope _globalScope;

    public event EventHandler<LabelCheckPointEventArgs>? LabelCheckPointReached;

    private bool _returnIndicator;

    public LimitCInterpreter(Dictionary<string, FunctionDef> functionDefs, Scope glocalScope, Scope? localScope = null, MemoryStorage? memoryStorage = null)
    {

        FunctionDefs = functionDefs;
        _globalScope = glocalScope;

        if (localScope != null)
            _scopes.Push(localScope);

        MemoryStorage = memoryStorage ?? new MemoryStorage();
    }

    public LimitCInterpreter()
    {
        FunctionDefs = new Dictionary<string, FunctionDef>();
        _globalScope = new Scope();
        MemoryStorage = new MemoryStorage();
    }

    public object? evaluate(LimitCParser.ProgContext program) 
    {
        var functionDetector = new LimitCFunctionTreeBuilder();
        functionDetector.Visit(program);

        return this.Visit(program);
    }

    #region GlobaFuncDef And CodeBlock (non-global)
    public override object? VisitFuncDef(LimitCParser.FuncDefContext context)
    {

        var cb = context.codeBlock();

        // Bei erreichen einer Funktionsdefintion, soll diese im Falle von main direkt abgerbeitet, sonst ignoriert werden
        // Funktionsdefintion erfolgt in vorgelagterten Schritt
        if (cb != null && context.ID().GetText() == "main")
        {
            Visit(cb);
        }

        return null;
    }

    public override object? VisitCodeBlock([NotNull] LimitCParser.CodeBlockContext context)
    {
        Scope? cs = null;

        if (_scopes.Count == 0)
        {
            _scopes.Push(new Scope());
        }
        else
        {
            cs = _scopes.Peek();
            cs.AddSubScope();
        }

        var ret = VisitChildren(context);


        if (cs == null)
        {
            _scopes.Pop();
        }
        else
        {
            cs.RemoveSubScope();
        }

        return ret;
    }

    protected override bool ShouldVisitNextChild(IRuleNode node, object? currentResult)
    {
        return !_returnIndicator;
    }

    #endregion

    #region Label

    private readonly Regex _labelRegex = new("Label[\\s]*([0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override object? VisitLabel([NotNull] LimitCParser.LabelContext context)
    {

        var m = _labelRegex.Match(context.LABEL().GetText());

        if (!m.Success) 
            return null;

        int labelID;

        if (int.TryParse(m.Groups[1].Value, out labelID) && labelID <= 0)
            return null;

        var cscope = _scopes.Peek();

        var visibleVariables = new Dictionary<string, int>();

        foreach (var (name, addr) in _globalScope.vars)
        {
            visibleVariables[name] = addr;
        }
        foreach (var (name, addr) in cscope.vars)
        {
            visibleVariables[name] = addr;
        }
        foreach (var scope in cscope.SubScopes)
        {
            foreach (var (name, addr) in scope.vars)
            {
                visibleVariables[name] = addr;
            }
        }

        LabelCheckPointReached?.Invoke(this, new LabelCheckPointEventArgs(labelID, visibleVariables, MemoryStorage));

        return null;
    }

    #endregion

    #region varDef & varAsign

    public override object? VisitVarDef([NotNull] LimitCParser.VarDefContext context)
    {

        var type = context.type().GetText();

        // für jeden Bezeichner der Defintion
        foreach (var def in context.varAssignDef())
        {
            // Name der Variable
            var name = def.ID().GetText();

            // aktueller Scope
            Scope currentScope = _scopes.Count > 0 ? _scopes.Peek() : _globalScope;

            // Initialisierungswert
            object? val = null;

            // Wenn Kein Zeiger -> mit 0 initialisieren
            if (!type.Contains('*'))
            {
                // no pointer var -> initialize with zero
                val = 0;
            }

            // variable in Scope hinzufügen und in Speicher ablegen
            currentScope.AddVar(name,  MemoryStorage.AddToMemory(type, val));
            // Zuweisung der Variable
            Visit(def);
        }

        return null;

    }

    public override object? VisitVarAssignDef(LimitCParser.VarAssignDefContext context)
    {
        // Zuweisungs Ausdruck
        var exp = context.expr();

        // Wenn keine Zuweisung vorhanden -> return
        if (exp == null)
            return null;

        // Variablenname holen
        var varName = context.ID().GetText();

        // aktuellen Scope ermitteln
        var currentScope = FindScopeForVar(varName);

        // Expression bearbeiten
        var value = Visit(exp);

        //Wenn expression zu einem zuweisbaren (nicht null) Wert führt -> zuweisen
        if (value != null)
        {
            MemoryStorage.ChangeInMemory(currentScope.GetAdress(varName), value);
        }
        
        return null;
    }

    #endregion

    #region constant

    public override object VisitIntegerConstant([NotNull] LimitCParser.IntegerConstantContext context)
    {
        return int.Parse(context.GetText());
    }

    public override object VisitDoubleConstant([NotNull] LimitCParser.DoubleConstantContext context)
    {
        return double.Parse($"{context.GetText()}", CultureInfo.InvariantCulture);
    }

    public override object VisitCharConstant([NotNull] LimitCParser.CharConstantContext context)
    {

        var s = context.GetText()[^2..^1];

        if (s.Length <= 0)
        {
            // leere char constants sind nicht zulässig
            throw new NotSupportedException("leere Chars sind nciht zulässig!");
        }

        return char.Parse(s);
    }

    public override object VisitStringConstant([NotNull] LimitCParser.StringConstantContext context)
    {
        return context.GetText()[1..^1];
    }

    #endregion

    #region expression (but constant)
    

    public override object? VisitLValExpression(LimitCParser.LValExpressionContext context)
    {

        var val = Visit(context.lvalue());

        if (val is AddrValue rAddrValue)
        {
            return MemoryStorage.GetFromMemory(rAddrValue.Address);
        }

        return val;


    }

    public override object? VisitFuncCall(LimitCParser.FuncCallContext context)
    {
        object? ret = null;
        var fname = context.ID().GetText(); // Funktionsname

        if (FunctionDefs.TryGetValue(fname, out var functionDef)) // Funktion definiert?
        {
            var nlc = new Scope();  // neuen Scope erstellen

            if (context.paramList() != null) // wurden Parameter übergeben?
            {
                var parameters = context.paramList().param(); // Liste der übergebenen Parameter

                for (var index = 0; index < parameters.Length; index++)
                {
                    var paramContext = parameters[index];
                    var val = Visit(paramContext.expr()); // Ausdruck auswerten (Variable auflösen, Constant auswerten, etc.)
                    var arg = functionDef.Arguments[index];

                    nlc.AddVar(arg.name, MemoryStorage.AddToMemory(arg.type, val)); // Variable in neuen Scope einfügen und im Speicher neu ablegen
                }
            }
            
            _scopes.Push(nlc); // we push the function's local scope
            ret = this.Visit(functionDef.ParseTree); // Abarbeitung Funktion durchführen
            _scopes.Pop(); // we pop the function's local scope
        }

        return ret; // Rückgabewerte zurückgeben

    }

    public override object? VisitParenthesesExpression([NotNull] LimitCParser.ParenthesesExpressionContext context)
    {
        return Visit(context.expr());
    }

    public override object? VisitUnaryPlusExpression([NotNull] LimitCParser.UnaryPlusExpressionContext context)
    {
        return Visit(context.expr());
    }

    public override object? VisitUnaryNegationExpression([NotNull] LimitCParser.UnaryNegationExpressionContext context)
    {

        object? ret = Visit(context.expr());

        if (ret != null)
        {
            if (ret is int retint)
                return -retint;
            if (ret is double retdouble)
                return -retdouble;
            if (ret is char retchar)
                return -retchar;
        }

        throw new InvalidOperationException($"Negation konnte nicht durchgeführt werden. Evtl. null Negation? Auruck: {context.GetText()}"); // in C this is partly supported, but unpredictable!

    }

    public override object? VisitCastExpression([NotNull] LimitCParser.CastExpressionContext context)
    {

        var val = Visit(context.expr());

        var nt = new TypedValue(context.type().GetText(), val);

        return nt.Value;
    }

    public override object VisitMulDivExpression([NotNull] LimitCParser.MulDivExpressionContext context)
    {

        object? l = Visit(context.expr(0)),
                r = Visit(context.expr(1));

        if (context.AST() != null)
        {
            if (l is int lint && r is int rint) // beides ints
            {
                return lint * rint;
            }
            if (l is double ldouble && r is double rdouble) // beides double
            {
                return ldouble * rdouble;
            }
            if (l is double ldouble2 && r is int rint2) // links ist double
            {
                return ldouble2 * rint2;
            }
            if (l is int lint2 && r is double rdouble2) // rechts is double
            {
                return lint2 * rdouble2;
            }
        }
        else if (context.DIVOP() != null)
        {

            if (l is int lint3 && r is int rint3)
            {
                return lint3 / rint3;
            }
            if (l is double ldouble && r is double rdouble)
            {
                return ldouble / rdouble;
            }
            if (l is double ldouble2 && r is int rint2)
            {
                return ldouble2 / rint2;
            }
            if (l is int lint2 && r is double rdouble2)
            {
                return lint2 / rdouble2;
            }
        }

        throw new InvalidOperationException($"Kann keine Multiplikation/Division durchführen für die Typen {l?.GetType()} und {r?.GetType()} im Ausdruck: {context.GetText()}");

    }

    public override object? VisitAddSubExpression([NotNull] LimitCParser.AddSubExpressionContext context)
    {


        object? l = Visit(context.expr(0)),
                r = Visit(context.expr(1));


        // Exception for char, beacuse: (char) is int == false,  --> calc char as int.
        // no need more, char is stored as int
        //if (l is char lchar)
        //{
        //    l = (int)lchar;
        //}
        //if (r is char rchar)
        //{
        //    r = (int)rchar;
        //}


        if (context.ADDOP() != null)
        {
            if (l is int lint && r is int rint)
            {
                return lint + rint;
            }
            if (l is double ldouble && r is double rdouble)
            {
                return ldouble + rdouble;
            }
            if (l is double ldouble2 && r is int rint2)
            {
                return ldouble2 + rint2;
            }
            if (l is int lint2 && r is double rdouble2)
            {
                return lint2 + rdouble2;
            }
        }
        else if (context.SUBOP() != null)
        {

            if (l is int lint3 && r is int rint3)
            {
                return lint3 - rint3;
            }
            if (l is double ldouble && r is double rdouble)
            {
                // einfach, beides sind double
                return ldouble - rdouble;
            }
            if (l is double ldouble2 && r is int rint2)
            {
                return ldouble2 - rint2;
            }
            if (l is int lint2 && r is double rdouble2)
            {
                return lint2 - rdouble2;
            }
        }

        throw new InvalidOperationException($"Kann keine Addition/Subtraktion durchführen für die Typen {l?.GetType()} und {r?.GetType()} im Ausdruck: {context.GetText()}");
    }

    #endregion

    #region "lval expre" 


    public override object? VisitVarExpression([NotNull] LimitCParser.VarExpressionContext context)
    {
        var varName = context.ID().GetText();

        if (varName == "NULL")
        {
            return null;
        }

        var currentScope = FindScopeForVar(varName);

        int addr = currentScope.GetAdress(varName);

        return new AddrValue(addr);

    }

    public override object VisitPostIncrementExpression([NotNull] LimitCParser.PostIncrementExpressionContext context)
    {

        var left = Visit(context.lvalue());

        if (left is AddrValue lval)
        {
            var addr = lval.Address;
            object? val = MemoryStorage.GetFromMemory(addr);

            if (val != null)
            {
                if (val is int retint)
                {
                    MemoryStorage.ChangeInMemory(addr, retint + 1);
                    return retint;
                }
                if (val is double retdouble)
                {
                    MemoryStorage.ChangeInMemory(addr, retdouble + 1);
                    return retdouble;
                }
                if (val is char retchar)
                {
                    MemoryStorage.ChangeInMemory(addr, retchar + 1);
                    return retchar;
                }
            }

        }

        throw new InvalidOperationException($"Der Ausdruck {context.GetText()} kan nicht inkrementiert werden!");

    }

    public override object VisitPostDecrementExpression([NotNull] LimitCParser.PostDecrementExpressionContext context)
    {

        var left = Visit(context.lvalue());

        if (left is AddrValue lval)
        {
            var addr = lval.Address;
            object? val = MemoryStorage.GetFromMemory(addr);

            if (val != null)
            {
                if (val is int retint)
                {
                    MemoryStorage.ChangeInMemory(addr, retint - 1);
                    return retint;
                }
                if (val is double retdouble)
                {
                    MemoryStorage.ChangeInMemory(addr, retdouble - 1);
                    return retdouble;
                }
                if (val is char retchar)
                {
                    MemoryStorage.ChangeInMemory(addr, retchar - 1);
                    return retchar;
                }
            }

        }

        throw new InvalidOperationException($"Der Ausdruck {context.GetText()} kan nicht dekrementiert werden!");
    }

    public override object? VisitPreIncrementExpression([NotNull] LimitCParser.PreIncrementExpressionContext context)
    {

        var left = Visit(context.lvalue());

        if (left is AddrValue lval)
        {
            var addr = lval.Address;
            object? val = MemoryStorage.GetFromMemory(addr);

            if (val != null)
            {
                if (val is int retint)
                {
                    MemoryStorage.ChangeInMemory(addr, retint + 1);
                    return MemoryStorage.GetFromMemory(addr);
                }
                if (val is double retdouble)
                {
                    MemoryStorage.ChangeInMemory(addr, retdouble + 1);
                    return MemoryStorage.GetFromMemory(addr);
                }
                if (val is char retchar)
                {
                    MemoryStorage.ChangeInMemory(addr, retchar + 1);
                    return MemoryStorage.GetFromMemory(addr);
                }
            }

        }

        throw new InvalidOperationException($"Der Ausdruck {context.GetText()} kan nicht inkrementiert werden!");
    }

    public override object? VisitPreDecrementExpression([NotNull] LimitCParser.PreDecrementExpressionContext context)
    {


        var left = Visit(context.lvalue());

        if (left is AddrValue lval)
        {
            var addr = lval.Address;
            object? val = MemoryStorage.GetFromMemory(addr);

            if (val != null)
            {
                if (val is int retint)
                {
                    MemoryStorage.ChangeInMemory(addr, retint - 1);
                    return MemoryStorage.GetFromMemory(addr);
                }
                if (val is double retdouble)
                {
                    MemoryStorage.ChangeInMemory(addr, retdouble - 1);
                    return MemoryStorage.GetFromMemory(addr);
                }
                if (val is char retchar)
                {
                    MemoryStorage.ChangeInMemory(addr, retchar - 1);
                    return MemoryStorage.GetFromMemory(addr);
                }
            }

        }

        throw new InvalidOperationException($"Der Ausdruck {context.GetText()} kan nicht dekrementiert werden!");
    }

    public override object? VisitLparExpression(LimitCParser.LparExpressionContext context)
    {
        return Visit(context.lvalue());
    }

    public override object? VisitAmpExpression(LimitCParser.AmpExpressionContext context)
    {
        var val = Visit(context.lvalue());

        if (val is AddrValue lval)
        {
            return lval.Address;
        }

        return null;

    }

    public override object? VisitAstExpression(LimitCParser.AstExpressionContext context)
    {

        var val = Visit(context.lvalue());

        if (val is AddrValue lval)
        {
            var v = MemoryStorage.GetFromMemory(lval.Address);
            if (v is int addr)
            {
                return new AddrValue(addr);
            }

        }

        return null;

    }

    #endregion

    #region termexpr

    public override object? VisitReturnExpression(LimitCParser.ReturnExpressionContext context)
    {
        var ret = Visit(context.expr());
        _returnIndicator = true;
        return ret;
    }

    #endregion

    #region Assignments

    public override object? VisitVarAssignment(LimitCParser.VarAssignmentContext context)
    {
        var ass = context;

        var left = Visit(ass.lvalue());

        if (left is AddrValue lval)
        {

            var exp = ass.expr();

            var value = Visit(exp);

            MemoryStorage.ChangeInMemory(lval.Address, value);
            return value;
        }

        return null;
    }

    public override object? VisitAddAssignment(LimitCParser.AddAssignmentContext context)
    {

        var left = Visit(context.lvalue());
        
        if (left is AddrValue lval)
        {
            var right = Visit(context.expr());
            var oval = MemoryStorage.GetFromMemory(lval.Address);
            if (oval is int lint && right is int rint)
            {
                MemoryStorage.ChangeInMemory(lval.Address, lint + rint);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is double ldouble && right is double rdouble)
            {
                MemoryStorage.ChangeInMemory(lval.Address, ldouble + rdouble);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is double ldouble2 && right is int rint2)
            {
                MemoryStorage.ChangeInMemory(lval.Address, ldouble2 + rint2);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is int lint2 && right is double rdouble2)
            {
                MemoryStorage.ChangeInMemory(lval.Address, lint2 + rdouble2);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
        }

        return null;
    }

    public override object? VisitSubAssignment(LimitCParser.SubAssignmentContext context)
    {
        var left = Visit(context.lvalue());

        if (left is AddrValue lval)
        {
            var right = Visit(context.expr());
            var oval = MemoryStorage.GetFromMemory(lval.Address);
            if (oval is int lint && right is int rint)
            {
                MemoryStorage.ChangeInMemory(lval.Address, lint - rint);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is double ldouble && right is double rdouble)
            {
                MemoryStorage.ChangeInMemory(lval.Address, ldouble - rdouble);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is double ldouble2 && right is int rint2)
            {
                MemoryStorage.ChangeInMemory(lval.Address, ldouble2 - rint2);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is int lint2 && right is double rdouble2)
            {
                MemoryStorage.ChangeInMemory(lval.Address, lint2 - rdouble2);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
        }

        return null;
    }

    public override object? VisitMultAssignment(LimitCParser.MultAssignmentContext context)
    {
        var left = Visit(context.lvalue());

        if (left is AddrValue lval)
        {
            var right = Visit(context.expr());
            var oval = MemoryStorage.GetFromMemory(lval.Address);
            if (oval is int lint && right is int rint)
            {
                MemoryStorage.ChangeInMemory(lval.Address, lint * rint);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is double ldouble && right is double rdouble)
            {
                MemoryStorage.ChangeInMemory(lval.Address, ldouble * rdouble);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is double ldouble2 && right is int rint2)
            {
                MemoryStorage.ChangeInMemory(lval.Address, ldouble2 * rint2);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is int lint2 && right is double rdouble2)
            {
                MemoryStorage.ChangeInMemory(lval.Address, lint2 * rdouble2);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
        }

        return null;
    }

    public override object? VisitDivAssignment(LimitCParser.DivAssignmentContext context)
    {
        var left = Visit(context.lvalue());

        if (left is AddrValue lval)
        {
            var right = Visit(context.expr());
            var oval = MemoryStorage.GetFromMemory(lval.Address);
            if (oval is int lint && right is int rint)
            {
                MemoryStorage.ChangeInMemory(lval.Address, lint / rint);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is double ldouble && right is double rdouble)
            {
                MemoryStorage.ChangeInMemory(lval.Address, ldouble / rdouble);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is double ldouble2 && right is int rint2)
            {
                MemoryStorage.ChangeInMemory(lval.Address, ldouble2 / rint2);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
            else if (oval is int lint2 && right is double rdouble2)
            {
                MemoryStorage.ChangeInMemory(lval.Address, lint2 / rdouble2);
                return MemoryStorage.GetFromMemory(lval.Address);
            }
        }

        return null;
    }

    #endregion

    #region helper

    private Scope FindScopeForVar(string varName)
    {

        Scope scope;

        if (_scopes.Count > 0)
        {
            scope = _scopes.Peek();

            if (scope.ContainsVar(varName))
                return scope;
        }

        scope = _globalScope;

        if (scope.ContainsVar(varName))
            return scope;


        throw new InvalidOperationException($"Variable {varName} wurde in keinem verfügabren Context definiert!");
    }

    #endregion

}