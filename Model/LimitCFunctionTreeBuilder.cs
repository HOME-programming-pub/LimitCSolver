using Model.SubTypes;

namespace Model;

public class LimitCFunctionTreeBuilder : LimitCBaseVisitor<object?>
{

    #region Function detection

    public Dictionary<string, FunctionDef> FunctionDefs = new();

    public override object? VisitFuncDef(LimitCParser.FuncDefContext context)
    {

        if (context.codeBlock() != null) // ignorieren von Prototypen oder Deklarationen
        {

            var parameters = new List<(string type, string name)>();

            var paramDef = context.paramListDef()?.paramDef(); // Liste von Funktionsparametern
            if (paramDef != null)
                foreach (var paramDefContext in paramDef) // f�r jeden Parameter in der Definition
                {
                    if(paramDefContext.type().GetText() != "void")
                        parameters.Add((paramDefContext.type().GetText(), paramDefContext.ID().GetText())); // Parameterliste mit Parametername und Typ bef�llen
                }

            // erkannte Funnktionsdefinition der Liste hinzuf�gen
            FunctionDefs.Add(context.ID().GetText(), new FunctionDef(context.ID().GetText(), context.type().GetText(), parameters, context.codeBlock()));
            Console.WriteLine($"detect function definition: {context.ID().GetText()}");
        }

        return null;
    }

    #endregion

}