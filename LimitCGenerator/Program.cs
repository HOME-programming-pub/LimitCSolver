using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.Data;

namespace LimitCSolver.LimitCGenerator;

public class VariableInfo
{
    public string Type { get; set; }
    public float Value { get; set; }

    public override string ToString()
    {
        return $"{Type}: {Value}";
    }
}

public class CodeGenerator
{
    private Random rand = new Random();
    private DifficultySettings settings;

    // Debug-Parameter //
    private static bool debug = false;
    private static bool debugAddition = true;
    private static bool debugSubtraction = true;
    private static bool debugMultiplication = true;
    private static bool debugDivision = true;

    // Aufgaben-Parameter //
    private int minDepth;
    private int maxDepth;
    private int minVarCount;
    private int maxVarCount;
    private int minLabel;
    private int maxLabel;
    private static bool allowGlobalVariables;
    private static bool allowVariableDeclaration;
    private static bool allowVariableAssignment;
    private static bool allowShadowVariables;
    private static bool allowAddition;
    private static bool allowSubtraction;
    private static bool allowMultiplication;
    private static bool allowDivision;
    private static bool allowIncrementDecrement;
    private static bool allowExplicitTypecasting;
    private bool[] operationsEnabled;
    private List<string> types = new List<string> { "int", "float", "short", "long", "char" };

    // Konstruktor, der die Einstellungen lädt //
    public CodeGenerator(DifficultySettings settings)
    {
        this.settings = settings;

        minDepth = settings.MinDepth;
        maxDepth = settings.MaxDepth;
        minVarCount = settings.MinVarCount;
        maxVarCount = settings.MaxVarCount;
        minLabel = settings.MinLabel;
        maxLabel = settings.MaxLabel;
        allowGlobalVariables = settings.AllowGlobalVariables;
        allowVariableDeclaration = settings.AllowVariableDeclaration;
        allowVariableAssignment = settings.AllowVariableAssignment;
        allowShadowVariables = settings.AllowShadowVariables;
        allowAddition = settings.AllowAddition;
        allowSubtraction = settings.AllowSubtraction;
        allowMultiplication = settings.AllowMultiplication;
        allowDivision = settings.AllowDivision;
        allowIncrementDecrement = settings.AllowIncrementDecrement;
        allowExplicitTypecasting = settings.AllowExplicitTypecasting;

        operationsEnabled = new bool[] {
            allowVariableDeclaration,
            allowVariableAssignment,
            allowShadowVariables,
            allowAddition,
            allowSubtraction,
            allowMultiplication,
            allowDivision
        };
    }


    public void GenerateCode()
    {
        StringBuilder code = new StringBuilder();

        List<Dictionary<char, VariableInfo>> scopeStack = new List<Dictionary<char, VariableInfo>>();
        Dictionary<char, VariableInfo> globalScope = new Dictionary<char, VariableInfo>();
        Dictionary<char, VariableInfo> currentScope = new Dictionary<char, VariableInfo>();

        // Mögliche Aktionen konfigurieren
        List<int> possibleActions = new List<int>();
        for (int j = 0; j < operationsEnabled.Length; j++)
        {
            if (operationsEnabled[j])
            {
                possibleActions.Add(j);
            }
        }

        string indent = "    ";

        code.Append("{\"Code\":\"");

        // Generiere globale Variablen
        if (allowGlobalVariables)
        {
            int numGlobals = rand.Next(3);
            for (int j = 0; j < numGlobals; j++)
            {
                HandleNewVariable(code, "", new Dictionary<char, VariableInfo>(), globalScope, types, scopeStack);
            }
        }

        code.Append("\\r\\n");
        scopeStack.Add(globalScope);

        // Generiere Main-Funktion
        code.Append("int main(void)\\r\\n{\\r\\n");

        // Generiere Code-Blöcke
        int currentDepth = 1;
        int labelCount = rand.Next(minLabel, maxLabel + 1);
        int i = 1;

        while (i <= labelCount)                                             // Schleife für die Anzahl der Labels
        {
            int randomDepth = rand.Next(minDepth + 1, maxDepth + 2);         // Zufällige Tiefe pro Block (1 ist für globalScope reserviert)                 
            for (int d = 0; d < randomDepth; d++)
            {
                if (i > maxLabel) break;                                    // Verlässt die Schleife, wenn die maximale Anzahl von Labels erreicht ist

                AddBlock(code, ref indent, ref currentDepth, scopeStack);   // Fügt einen Block (Scope) hinzu

                currentScope = scopeStack[currentDepth - 1];                // Aktueller Scope
                HandleOperations(code, indent, ref currentDepth, possibleActions, currentScope, types, scopeStack);
                AddLabel(code, indent, i);
                i++;
            }

            // Schließe Blöcke
            if (currentDepth >= randomDepth)
            {
                for (int j = currentDepth; j > 1; j--)
                {
                    CloseBlock(code, ref indent, ref currentDepth, scopeStack);

                    // Füge zuffälig Operationen hinzu, wenn der Block geschlossen wird
                    if (rand.Next(2) == 0 && i <= maxLabel)
                    {
                        currentScope = scopeStack[currentDepth - 1];
                        HandleOperations(code, indent, ref currentDepth, possibleActions, currentScope, types, scopeStack);
                        AddLabel(code, indent, i);
                        i++;
                    }
                    // Füge zuffälig ein Label hinzu, wenn der Block geschlossen wird
                    else if (rand.Next(2) == 0 && scopeStack.Count > 1 && i <= maxLabel)
                    {
                        AddLabel(code, indent, i);
                        i++;
                    }
                }
                currentDepth = 1;
            }
        }

        // Schließe restliche Blöcke und Main-Funktion
        while (currentDepth > 0)
        {
            CloseBlock(code, ref indent, ref currentDepth, scopeStack);

            // Füge zuffälig Operationen hinzu, wenn der Block geschlossen wird
            if (rand.Next(2) == 0 && i <= maxLabel && currentDepth > 0)
            {
                currentScope = scopeStack[currentDepth - 1];
                HandleOperations(code, indent, ref currentDepth, possibleActions, currentScope, types, scopeStack);
                AddLabel(code, indent, i);
                i++;
            }
            // Füge zuffälig ein Label hinzu, wenn der Block geschlossen wird
            else if (rand.Next(2) == 0 && scopeStack.Count > 1 && i <= maxLabel)
            {
                AddLabel(code, indent, i);
                i++;
            }
        }

        code.Append("\"}");
        try
        {
            string filePath = "..\\..\\..\\..\\_Modul_CreateLimitC\\Model\\output.lct.json";

            System.IO.File.WriteAllText(filePath, code.ToString());
            if (debug) { Console.WriteLine("Datei wurde erfolgreich geschrieben."); }
        }
        catch (Exception ex)
        {
            if (debug) { Console.WriteLine("Fehler beim Schreiben der Datei: " + ex.Message); }
        }

        return;
    }

    /****************************************************************************************************************
    Funktionen
    ****************************************************************************************************************/

    private void HandleOperations(StringBuilder code, string indent, ref int currentDepth, List<int> possibleActions, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        Dictionary<char, VariableInfo> otherScopes = new Dictionary<char, VariableInfo>();
        foreach (var scope in scopeStack)
        {
            foreach (var pair in scope)
            {
                if (!currentScope.ContainsKey(pair.Key))
                {
                    if (otherScopes.ContainsKey(pair.Key)) { otherScopes[pair.Key] = pair.Value; }
                    else { otherScopes.Add(pair.Key, pair.Value); }
                }
            }
        }

        int varCount = rand.Next(minVarCount, maxVarCount + 1);   // Zufällige Anzahl von Variablen pro Block

        // Schleife zur Erzeugung mehrerer Operationen pro Block
        for (int v = 0; v < varCount; v++)
        {
            int actionIndex = rand.Next(possibleActions.Count);
            int action = possibleActions[actionIndex];

            // Korrektur bei schweren Aufgaben (Wenn es mehr als 4 mögliche Aktionen gibt, dann wird die Wahrscheinlichkeit für eine Deklaration erhöht)
            if (possibleActions.Count > 4 && currentScope.Count + otherScopes.Count < 3)
            {
                if (otherScopes.Count != 0 && rand.Next(2) == 0) { HandleShadowVariable(code, indent, otherScopes, currentScope, types, scopeStack); }
                else { HandleNewVariable(code, indent, otherScopes, currentScope, types, scopeStack); }
            }
            // Deklaration
            else if (action == 0) { HandleNewVariable(code, indent, otherScopes, currentScope, types, scopeStack); }
            // Wertzuweisung (Wenn nicht geht -> Deklaration)
            else if (action == 1) { HandleAssignment(code, indent, otherScopes, currentScope, scopeStack); }
            // Schattenvariable (Wenn nicht geht -> Deklaration)
            else if (action == 2) { HandleShadowVariable(code, indent, otherScopes, currentScope, types, scopeStack); }
            // Addition (Wenn nicht geht -> Deklaration)
            else if (action == 3) { HandleAdditionSubtraction(false, true, code, indent, otherScopes, currentScope, types, scopeStack); }
            // Subtraktion (Wenn nicht geht -> Deklaration)
            else if (action == 4) { HandleAdditionSubtraction(false, false, code, indent, otherScopes, currentScope, types, scopeStack); }
            // Multiplikation (Wenn nicht geht -> Deklaration)
            else if (action == 5) { HandleMultiplicationDivision(false, true, code, indent, otherScopes, currentScope, types, scopeStack); }
            // Division (Wenn nicht geht -> Deklaration)
            else if (action == 6) { HandleMultiplicationDivision(false, false, code, indent, otherScopes, currentScope, types, scopeStack); }
        }
    }

    private void HandleMultiplicationDivision(bool isVariableDeclaration, bool isMultiplication, StringBuilder code, string indent, Dictionary<char, VariableInfo> otherScopes, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        // Wenn keine Variable verfügbar ist -> Deklaration
        if (otherScopes.Count == 0 && currentScope.Count == 0)
        {
            HandleNewVariable(code, indent, otherScopes, currentScope, types, scopeStack);
            return;
        }

        // Liste für mögliche Variablen
        List<KeyValuePair<char, VariableInfo>> combinedItemList = new List<KeyValuePair<char, VariableInfo>>(currentScope);

        foreach (var item in otherScopes)
        {
            if (!currentScope.ContainsKey(item.Key))
            {
                combinedItemList.Add(item);
            }
        }

        // Generierung einer neuen Variable oder Auswahl einer vorhandenen Variable
        char varName;
        string varType;
        // Wenn Variable deklariert werden soll, dann generiere eine neue Variable
        if (isVariableDeclaration)
        {
            var localVar = GenerateRandomVariable(types, otherScopes, currentScope);
            varType = localVar.Item1;
            varName = localVar.Item2;
        }
        // Ansonsten wähle eine zufällige Variable
        else
        {
            KeyValuePair<char, VariableInfo> selectedVar = combinedItemList[rand.Next(combinedItemList.Count)];
            varName = selectedVar.Key;
            varType = selectedVar.Value.Type;
        }

        int firstThreshold = isVariableDeclaration ? 0 : 1;

        // Wenn mehr als 1 Variable verfügbar ist, dann eine zufällige Variable
        if (combinedItemList.Count > firstThreshold)
        {
            KeyValuePair<char, VariableInfo> var1 = combinedItemList[rand.Next(combinedItemList.Count)];
            char var1Name = var1.Key;
            string var1Type = var1.Value.Type;
            float var1Value = var1.Value.Value;

            if (var1Value >= 1000)
            {
                if (isMultiplication) { HandleTwoRandomNumbersMultiplication(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack); }
                else { HandleTwoRandomNumbersDivision(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack); }
            }
            else
            {
                if (isMultiplication) { HandleOneVariableMultiplication(isVariableDeclaration, code, indent, varName, varType, var1Value, var1Type, var1Name, currentScope, types, scopeStack); }
                else { HandleOneVariableDivision(isVariableDeclaration, code, indent, varName, varType, var1Value, var1Type, var1Name, currentScope, types, scopeStack); }
            }
        }

        // Wenn nur 1 Variable verfügbar ist, dann zwei zufällige Variablen
        else
        {
            if (isMultiplication) { HandleTwoRandomNumbersMultiplication(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack); }
            else { HandleTwoRandomNumbersDivision(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack); }
        }
    }

    private void HandleOneVariableDivision(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, float var1Value, string var1Type, char var1Name, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        float result, randomValue1;
        List<float> divisors = new List<float> { 2, 4, 5, 10 };
        int castPosition = rand.Next(2);
        string castType = "";
        string operation1 = rand.Next(2) == 0 ? "++" : "--";
        string position1 = rand.Next(2) == 0 ? $"{operation1}{var1Name}" : $"{var1Name}{operation1}";
        bool shouldTypecasting = false;
        bool shouldIncrementDecrement = false;

        // Zufällige Entscheidung, ob Typecasting und/oder Increment/Decrement
        if (allowIncrementDecrement && rand.Next(2) == 0)
        {
            shouldIncrementDecrement = true;
            if (allowExplicitTypecasting && rand.Next(2) == 0)
            {
                shouldTypecasting = true;
            }
        }
        else if (allowExplicitTypecasting && rand.Next(2) == 0)
        {
            shouldTypecasting = true;
            if (allowIncrementDecrement && rand.Next(2) == 0)
            {
                shouldIncrementDecrement = true;
            }
        }

        // Wenn Typecasting -> Sicherstellen, dass der neue Typ sich vom alten Typ unterscheidet
        if (shouldTypecasting)
        {
            do
            {
                castType = types[rand.Next(types.Count)];
            } while (castType == var1Type);
        }

        // Wenn Inkrement/Dekrement -> Position ist sehr gut ich muss Wertebereich für Char nicht neu rechnen
        if (shouldIncrementDecrement)
        {
            if (position1.StartsWith("++")) var1Value++;
            if (position1.StartsWith("--")) var1Value--;
        }

        // Prüfen ob Division durch 0
        if (var1Value == 0)
        {
            HandleTwoRandomNumbersDivision(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack);
            return;
        }

        // Generierung einer Zufallszahl
        if (castPosition == 0)
        {
            if (varType == "char")
            {
                List<float> validDivisors = new List<float>();
                for (randomValue1 = -10; randomValue1 <= 10; randomValue1++)   // Suche nach gültigen Divisoren (meistens 1)
                {
                    if (var1Value / randomValue1 >= 97 && var1Value / randomValue1 <= 122) { validDivisors.Add(randomValue1); }
                }

                if (validDivisors.Count == 0)
                {
                    HandleTwoRandomNumbersDivision(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack);
                    return;
                }

                randomValue1 = validDivisors[rand.Next(validDivisors.Count)];
            }
            else { randomValue1 = divisors[rand.Next(divisors.Count)]; }
        }
        else
        {
            if (varType == "char")
            {
                List<float> validDivisors = new List<float>();
                for (randomValue1 = -10; randomValue1 <= 10; randomValue1++)   // Suche nach gültigen Divisoren (meistens 1)
                {
                    if (randomValue1 / var1Value >= 97 && randomValue1 / var1Value <= 122) { validDivisors.Add(randomValue1); }
                }

                if (validDivisors.Count == 0)
                {
                    HandleTwoRandomNumbersDivision(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack);
                    return;
                }

                randomValue1 = validDivisors[rand.Next(validDivisors.Count)];
            }
            else { randomValue1 = divisors[rand.Next(divisors.Count)] * var1Value; }
        }

        randomValue1 = (float)Math.Round(randomValue1, 2);

        // Berechnung des Ergebnisses und Generierung des Codes
        if (castPosition == 0)
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = (int)var1Value / randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value / randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType}){position1} / {randomValue1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Increment/Decrement with Typecasting {varType} {varName} = ({castType}){position1} / {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType}){position1} / {randomValue1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Increment/Decrement with Typecasting {varName} = ({castType}){position1} / {randomValue1}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = (int)var1Value / randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value / randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType}){var1Name} / {randomValue1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Typecasting {varType} {varName} = ({castType}){var1Name} / {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType}){var1Name} / {randomValue1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Typecasting: {varName} = ({castType}){var1Name} / {randomValue1}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value / randomValue1;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} / {randomValue1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Increment/Decrement {varType} {varName} = {position1} / {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} / {randomValue1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Increment/Decrement: {varName} = {position1} / {randomValue1}"); }
                }
            }
            else
            {
                result = var1Value / randomValue1;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} / {randomValue1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division {varType} {varName} = {var1Name} / {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} / {randomValue1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division: {varName} = {var1Name} / {randomValue1}"); }
                }
            }
        }
        else
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = (int)var1Value / randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value / randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} / ({castType}){position1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Increment/Decrement with Typecasting {varType} {varName} = {randomValue1} / ({castType}){position1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} / ({castType}){position1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Increment/Decrement with Typecasting: {varName} = {randomValue1} / ({castType}){position1}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = (int)var1Value / randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value / randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} / ({castType}){var1Name};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Typecasting {varType} {varName} = {randomValue1} / ({castType}){var1Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} / ({castType}){var1Name};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Typecasting: {varName} = {randomValue1} / ({castType}){var1Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = randomValue1 / var1Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} / {position1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Increment/Decrement {varType} {varName} = {randomValue1} / {position1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} / {position1};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division with Increment/Decrement: {varName} = {randomValue1} / {position1}"); }
                }
            }
            else
            {
                result = randomValue1 / var1Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} / {var1Name};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division {varType} {varName} = {randomValue1} / {var1Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} / {var1Name};\\r\\n");
                    if (debug && debugDivision) { Console.WriteLine($"* Division: {varName} = {randomValue1} / {var1Name}"); }
                }
            }
        }

        if (shouldIncrementDecrement)
        {
            if (position1.EndsWith("++")) var1Value++;
            if (position1.EndsWith("--")) var1Value--;
        }

        // Aktualisierung der Werte im ScopeStack
        result = (float)Math.Round(result, 2);
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        // Sonst -> Aktualisierung des Wertes im ScopeStack
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }
        // Inkrement/Dekrement -> Aktualisierung des Wertes im ScopeStack
        if (shouldIncrementDecrement && varName != var1Name) { UpdateVariableInScopeStack(var1Name, var1Value, var1Type, scopeStack); }
    }

    private void HandleOneVariableMultiplication(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, float var1Value, string var1Type, char var1Name, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        float result, randomValue1;
        int castPosition = rand.Next(2);
        string castType = "";
        string operation1 = rand.Next(2) == 0 ? "++" : "--";
        string position1 = rand.Next(2) == 0 ? $"{operation1}{var1Name}" : $"{var1Name}{operation1}";
        bool shouldTypecasting = false;
        bool shouldIncrementDecrement = false;

        // Zufällige Entscheidung, ob Typecasting und/oder Increment/Decrement
        if (allowIncrementDecrement && rand.Next(2) == 0)
        {
            shouldIncrementDecrement = true;
            if (allowExplicitTypecasting && rand.Next(2) == 0)
            {
                shouldTypecasting = true;
            }
        }
        else if (allowExplicitTypecasting && rand.Next(2) == 0)
        {
            shouldTypecasting = true;
            if (allowIncrementDecrement && rand.Next(2) == 0)
            {
                shouldIncrementDecrement = true;
            }
        }

        // Wenn Typecasting -> Sicherstellen, dass der neue Typ sich vom alten Typ unterscheidet
        if (shouldTypecasting)
        {
            do
            {
                castType = types[rand.Next(types.Count)];
            } while (castType == var1Type);
        }

        // Wenn Inkrement/Dekrement -> Position ist sehr gut ich muss Wertebereich für Char nicht neu rechnen
        if (shouldIncrementDecrement)
        {
            if (position1.StartsWith("++")) var1Value++;
            if (position1.StartsWith("--")) var1Value--;
        }

        // Generierung einer Zufallszahl
        if (varType == "char")
        {
            List<float> validMultipliers = new List<float>();
            for (randomValue1 = -10; randomValue1 <= 10; randomValue1++)   // Suche nach gültigen Multiplikatoren
            {
                result = var1Value * randomValue1;
                if (result >= 97 && result <= 122) { validMultipliers.Add(randomValue1); }
            }

            if (validMultipliers.Count == 0)
            {
                HandleTwoRandomNumbersMultiplication(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack);
                return;
            }

            randomValue1 = validMultipliers[rand.Next(validMultipliers.Count)];
        }
        else
        {
            do { randomValue1 = rand.Next(-5, 6); }
            while (randomValue1 == 0);
        }

        // Berechnung des Ergebnisses und Generierung des Codes
        if (castPosition == 0)
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = (int)var1Value * randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value * randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType}){position1} * {randomValue1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Increment/Decrement with Typecasting {varType} {varName} = ({castType}){position1} * {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType}){position1} * {randomValue1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Increment/Decrement with Typecasting {varName} = ({castType}){position1} * {randomValue1}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = (int)var1Value * randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value * randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType}){var1Name} * {randomValue1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Typecasting {varType} {varName} = ({castType}){var1Name} * {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType}){var1Name} * {randomValue1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Typecasting: {varName} = ({castType}){var1Name} * {randomValue1}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value * randomValue1;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} * {randomValue1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Increment/Decrement {varType} {varName} = {position1} * {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} * {randomValue1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Increment/Decrement: {varName} = {position1} * {randomValue1}"); }
                }
            }
            else
            {
                result = var1Value * randomValue1;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} * {randomValue1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication {varType} {varName} = {var1Name} * {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} * {randomValue1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication: {varName} = {var1Name} * {randomValue1}"); }
                }
            }
        }
        else
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = (int)var1Value * randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value * randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} * ({castType}){position1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Increment/Decrement with Typecasting {varType} {varName} = {randomValue1} * ({castType}){position1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} * ({castType}){position1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Increment/Decrement with Typecasting: {varName} = {randomValue1} * ({castType}){position1}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = (int)var1Value * randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value * randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} * ({castType}){var1Name};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Typecasting {varType} {varName} = {randomValue1} * ({castType}){var1Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} * ({castType}){var1Name};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Typecasting: {varName} = {randomValue1} * ({castType}){var1Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = randomValue1 * var1Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} * {position1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Increment/Decrement {varType} {varName} = {randomValue1} * {position1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} * {position1};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication with Increment/Decrement: {varName} = {randomValue1} * {position1}"); }
                }
            }
            else
            {
                result = randomValue1 * var1Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} * {var1Name};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication {varType} {varName} = {randomValue1} * {var1Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} * {var1Name};\\r\\n");
                    if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication: {varName} = {randomValue1} * {var1Name}"); }
                }
            }
        }

        if (shouldIncrementDecrement)
        {
            if (position1.EndsWith("++")) var1Value++;
            if (position1.EndsWith("--")) var1Value--;
        }

        // Aktualisierung der Werte im ScopeStack
        result = (float)Math.Round(result, 2);
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        // Sonst -> Aktualisierung des Wertes im ScopeStack
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }
        // Inkrement/Dekrement -> Aktualisierung des Wertes im ScopeStack
        if (shouldIncrementDecrement && varName != var1Name) { UpdateVariableInScopeStack(var1Name, var1Value, var1Type, scopeStack); }
    }

    private void HandleTwoRandomNumbersDivision(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        float result, randomValue1, randomValue2, baseValue;
        List<float> divisors = new List<float> { 2, 4, 5, 10 };

        // Generierung von zwei Zufallszahlen
        if (varType == "char")
        {
            do
            {
                randomValue1 = divisors[rand.Next(divisors.Count)];
                baseValue = rand.Next(1, 1001) / 2.0f;
                randomValue2 = baseValue * randomValue1;
            } while (randomValue2 / randomValue1 < 97 || randomValue2 / randomValue1 > 122);
        }
        else
        {
            randomValue1 = divisors[rand.Next(divisors.Count)];
            baseValue = rand.Next(1, 101) / 2.0f;
            randomValue2 = baseValue * randomValue1;
        }

        // Berechnung des Ergebnisses
        result = randomValue2 / randomValue1;
        result = (float)Math.Round(result, 2);

        // Generierung des Codes
        if (isVariableDeclaration)
        {
            code.Append($"{indent}{varType} {varName} = {randomValue2} / {randomValue1};\\r\\n");
            if (debug && debugDivision) { Console.WriteLine($"* Division: {varType} {varName} = {randomValue2} / {randomValue1}"); }
        }
        else
        {
            code.Append($"{indent}{varName} = {randomValue2} / {randomValue1};\\r\\n");
            if (debug && debugDivision) { Console.WriteLine($"* Division: {varName} = {randomValue2} / {randomValue1}"); }
        }

        // Aktualisierung des Ergebnisses im ScopeStack
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        // Ansonsten -> Aktualisierung des Werts im ScopeStack
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }
    }

    private void HandleTwoRandomNumbersMultiplication(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        float result, randomValue1, randomValue2;

        // Generierung von zwei Zufallszahlen
        if (varType == "char")
        {
            do
            {
                randomValue1 = rand.Next(-10, 11);      // einfache Zahlen weil keine Mathe Aufgaben
                randomValue2 = rand.Next(-10, 11);
            } while (randomValue1 * randomValue2 < 97 || randomValue1 * randomValue2 > 122);
        }
        else
        {
            randomValue1 = rand.Next(-10, 11);
            randomValue2 = rand.Next(-10, 11);
        }

        // Berechnung des Ergebnisses
        result = randomValue1 * randomValue2;
        result = (float)Math.Round(result, 2);

        // Generierung des Codes
        if (isVariableDeclaration)
        {
            code.Append($"{indent}{varType} {varName} = {randomValue1} * {randomValue2};\\r\\n");
            if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication: {varType} {varName} = {randomValue1} * {randomValue2}"); }
        }
        else
        {
            code.Append($"{indent}{varName} = {randomValue1} * {randomValue2};\\r\\n");
            if (debug && debugMultiplication) { Console.WriteLine($"* Multiplication: {varName} = {randomValue1} * {randomValue2}"); }
        }

        // Aktualisierung des Ergebnisses im ScopeStack
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        // Ansonsten -> Aktualisierung des Werts im ScopeStack
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }
    }

    private void HandleAdditionSubtraction(bool isVariableDeclaration, bool isAddition, StringBuilder code, string indent, Dictionary<char, VariableInfo> otherScopes, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        // Wenn keine Variable verfügbar ist -> Deklaration
        if (isVariableDeclaration == false && otherScopes.Count == 0 && currentScope.Count == 0)
        {
            HandleNewVariable(code, indent, otherScopes, currentScope, types, scopeStack);
            return;
        }

        // Liste für mögliche Variablen
        List<KeyValuePair<char, VariableInfo>> combinedItemList = new List<KeyValuePair<char, VariableInfo>>(currentScope);
        foreach (var item in otherScopes)
        {
            if (!currentScope.ContainsKey(item.Key)) { combinedItemList.Add(item); }
        }

        // Generierung einer neuen Variable oder Auswahl einer vorhandenen Variable
        char varName;
        string varType;

        if (isVariableDeclaration)
        {
            var localVar = GenerateRandomVariable(types, otherScopes, currentScope);
            varType = localVar.Item1;
            varName = localVar.Item2;
        }
        else
        {
            KeyValuePair<char, VariableInfo> selectedVar = combinedItemList[rand.Next(combinedItemList.Count)];
            varName = selectedVar.Key;
            varType = selectedVar.Value.Type;
        }

        int firstThreshold = isVariableDeclaration ? 1 : 2;
        int secondThreshold = isVariableDeclaration ? 0 : 1;

        if (combinedItemList.Count > firstThreshold) // Zwei Variablen
        {
            KeyValuePair<char, VariableInfo> var1 = combinedItemList[rand.Next(combinedItemList.Count)];
            char var1Name = var1.Key;
            string var1Type = var1.Value.Type;
            float var1Value = var1.Value.Value;

            KeyValuePair<char, VariableInfo> var2;
            do { var2 = combinedItemList[rand.Next(combinedItemList.Count)]; } while (var1Name == var2.Key);
            char var2Name = var2.Key;
            string var2Type = var2.Value.Type;
            float var2Value = var2.Value.Value;

            if (isAddition) { HandleTwoVariableAddition(isVariableDeclaration, code, indent, varName, varType, var1Value, var1Type, var1Name, var2Value, var2Type, var2Name, currentScope, types, scopeStack); }
            else { HandleTwoVariableSubtraction(isVariableDeclaration, code, indent, varName, varType, var1Value, var1Type, var1Name, var2Value, var2Type, var2Name, currentScope, types, scopeStack); }
        }
        else if (combinedItemList.Count > secondThreshold) // Eine zufällige Variable
        {
            KeyValuePair<char, VariableInfo> var1 = combinedItemList[rand.Next(combinedItemList.Count)];
            char var1Name = var1.Key;
            string var1Type = var1.Value.Type;
            float var1Value = var1.Value.Value;

            if (isAddition) { HandleOneVariableAddition(isVariableDeclaration, code, indent, varName, varType, var1Value, var1Type, var1Name, currentScope, types, scopeStack); }
            else { HandleOneVariableSubtraction(isVariableDeclaration, code, indent, varName, varType, var1Value, var1Type, var1Name, currentScope, types, scopeStack); }
        }
        else // Zwei zufällige Variablen
        {
            if (isAddition) { HandleTwoRandomNumbersAddition(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack); }
            else { HandleTwoRandomNumbersSubtraction(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack); }
        }
    }

    private void HandleTwoVariableSubtraction(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, float var1Value, string var1Type, char var1Name, float var2Value, string var2Type, char var2Name, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        // Char Operation möglich, wenn var1 und var2 getauscht werden -> passiert verdammt selten
        if (varType == "char" && var2Value - var1Value > 97 && var2Value - var1Value < 122)
        {
            (var1Name, var1Value, var1Type, var2Name, var2Value, var2Type) =
            (var2Name, var2Value, var2Type, var1Name, var1Value, var1Type);
        }

        // Keine Char Operation mit var1 - var2 möglich
        if (varType == "char" && (var1Value - var2Value < 99 || var1Value - var2Value > 120))
        {
            if (!CharValueOutOfRange(varType, var1Value))
            {
                HandleOneVariableSubtraction(isVariableDeclaration, code, indent, varName, varType, var1Value, var1Type, var1Name, currentScope, types, scopeStack);
                return;
            }
            else if (!CharValueOutOfRange(varType, var2Value))
            {
                HandleOneVariableSubtraction(isVariableDeclaration, code, indent, varName, varType, var2Value, var2Type, var2Name, currentScope, types, scopeStack);
                return;
            }
            else
            {
                HandleTwoRandomNumbersSubtraction(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack);
                return;
            }
        }

        float result;
        int castPosition = rand.Next(3);
        string castType1 = "";
        string castType2 = "";
        string operation1 = rand.Next(2) == 0 ? "++" : "--";
        string position1 = rand.Next(2) == 0 ? $"{operation1}{var1Name}" : $"{var1Name}{operation1}";
        string operation2 = rand.Next(2) == 0 ? "++" : "--";
        string position2 = rand.Next(2) == 0 ? $"{operation2}{var2Name}" : $"{var2Name}{operation2}";
        bool shouldTypecasting = false;
        bool shouldIncrementDecrement = false;

        // Zufällige Entscheidung, ob Typecasting und/oder Increment/Decrement
        if (allowIncrementDecrement && rand.Next(2) == 0)
        {
            shouldIncrementDecrement = true;
            if (allowExplicitTypecasting && rand.Next(2) == 0)
            {
                shouldTypecasting = true;
            }
        }
        else if (allowExplicitTypecasting && rand.Next(2) == 0)
        {
            shouldTypecasting = true;
            if (allowIncrementDecrement && rand.Next(2) == 0)
            {
                shouldIncrementDecrement = true;
            }
        }

        // Wenn Typecasting -> Sicherstellen, dass der neue Typ sich vom alten Typ unterscheidet
        if (shouldTypecasting)
        {
            do
            {
                castType1 = types[rand.Next(types.Count)];
            } while (castType1 == var1Type);

            do
            {
                castType2 = types[rand.Next(types.Count)];
            } while (castType2 == var2Type);
        }

        // Wenn Inkrement/Dekrement -> Berechnung des Ergebnisses und Generierung des Codes
        if (shouldIncrementDecrement)
        {
            if (position1.StartsWith("++")) var1Value++;
            if (position1.StartsWith("--")) var1Value--;
            if (position2.StartsWith("++")) var2Value++;
            if (position2.StartsWith("--")) var2Value--;
        }

        // Berechnung des Ergebnisses und Generierung des Codes
        if (castPosition == 0)
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = (int)var1Value - var2Value; }         // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value - var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType1}){position1} - {position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting: {varType} {varName} = ({castType1}){position1} - {position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType1}){position1} - {position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting: {varName} = ({castType1}){position1} - {position2}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = (int)var1Value - var2Value; }         // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value - var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType1}){var1Name} - {var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting: {varType} {varName} = ({castType1}){var1Name} - {var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType1}){var1Name} - {var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting: {varName} = ({castType1}){var1Name} - {var2Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value - var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} - {position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement: {varType} {varName} = {position1} - {position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} - {position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement: {varName} = {position1} - {position2}"); }
                }
            }
            else
            {
                result = var1Value - var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} - {var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction: {varType} {varName} = {var1Name} - {var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} - {var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction: {varName} = {var1Name} - {var2Name}"); }
                }
            }
        }
        else if (castPosition == 1)
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var2Type == "float") { result = var1Value - (int)var2Value; }         // Typecasting beachten -> var2Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value - var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} - ({castType2}){position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting: {varType} {varName} = {position1} - ({castType2}){position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} - ({castType2}){position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting: {varName} = {position1} - ({castType2}){position2}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var2Type == "float") { result = var1Value - (int)var2Value; }         // Typecasting beachten -> var2Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value - var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} - ({castType2}){var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting: {varType} {varName} = {var1Name} - ({castType2}){var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} - ({castType2}){var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting: {varName} = {var1Name} - ({castType2}){var2Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value - var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} - {position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement: {varType} {varName} = {position1} - {position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} - {position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement: {varName} = {position1} - {position2}"); }
                }
            }
            else
            {
                result = var1Value - var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} - {var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction: {varType} {varName} = {var1Name} - {var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} - {var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction: {varName} = {var1Name} - {var2Name}"); }
                }
            }
        }
        else
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float" && var2Type == "float") { result = (int)var1Value - (int)var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else if (var1Type == "float") { result = (int)var1Value - var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else if (var2Type == "float") { result = var1Value - (int)var2Value; } // Typecasting beachten -> var2Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value - var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType1}){position1} - ({castType2}){position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting: {varType} {varName} = ({castType1}){position1} - ({castType2}){position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType1}){position1} - ({castType2}){position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting: {varName} = ({castType1}){position1} - ({castType2}){position2}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float" && var2Type == "float") { result = (int)var1Value - (int)var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else if (var1Type == "float") { result = (int)var1Value - var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else if (var2Type == "float") { result = var1Value - (int)var2Value; } // Typecasting beachten -> var2Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value - var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType1}){var1Name} - ({castType2}){var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting: {varType} {varName} = ({castType1}){var1Name} - ({castType2}){var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType1}){var1Name} - ({castType2}){var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting: {varName} = ({castType1}){var1Name} - ({castType2}){var2Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value - var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} - {position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement: {varType} {varName} = {position1} - {position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} - {position2};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement: {varName} = {position1} - {position2}"); }
                }
            }
            else
            {
                result = var1Value - var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} - {var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction: {varType} {varName} = {var1Name} - {var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} - {var2Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction: {varName} = {var1Name} - {var2Name}"); }
                }
            }
        }

        if (shouldIncrementDecrement)
        {
            if (position1.EndsWith("++")) var1Value++;
            if (position1.EndsWith("--")) var1Value--;
            if (position2.EndsWith("++")) var2Value++;
            if (position2.EndsWith("--")) var2Value--;
        }

        // Aktualisierung der Werte im ScopeStack
        result = (float)Math.Round(result, 2);
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        // Sonst -> Aktualisierung der Variable im aktuellen Scope
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }
        // Wenn Inkrement/Dekrement -> Aktualisierung der anderen Variablen im ScopeStack
        if (shouldIncrementDecrement)
        {
            if (varName != var1Name) { UpdateVariableInScopeStack(var1Name, var1Value, var1Type, scopeStack); }
            if (varName != var2Name) { UpdateVariableInScopeStack(var2Name, var2Value, var2Type, scopeStack); }
        }
    }

    private void HandleTwoVariableAddition(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, float var1Value, string var1Type, char var1Name, float var2Value, string var2Type, char var2Name, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        // Keine Char Operation mit var1 + var2 möglich
        if (varType == "char" && (var1Value + var2Value < 99 || var1Value + var2Value > 120))
        {
            if (!CharValueOutOfRange(varType, var1Value))
            {
                HandleOneVariableAddition(isVariableDeclaration, code, indent, varName, varType, var1Value, var1Type, var1Name, currentScope, types, scopeStack);
                return;
            }
            else if (!CharValueOutOfRange(varType, var2Value))
            {
                HandleOneVariableAddition(isVariableDeclaration, code, indent, varName, varType, var2Value, var2Type, var2Name, currentScope, types, scopeStack);
                return;
            }
            else
            {
                HandleTwoRandomNumbersAddition(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack);
                return;
            }
        }

        float result;
        int castPosition = rand.Next(3);
        string castType1 = "";
        string castType2 = "";
        string operation1 = rand.Next(2) == 0 ? "++" : "--";
        string position1 = rand.Next(2) == 0 ? $"{operation1}{var1Name}" : $"{var1Name}{operation1}";
        string operation2 = rand.Next(2) == 0 ? "++" : "--";
        string position2 = rand.Next(2) == 0 ? $"{operation2}{var2Name}" : $"{var2Name}{operation2}";
        bool shouldTypecasting = false;
        bool shouldIncrementDecrement = false;

        // Zufällige Entscheidung, ob Typecasting und/oder Increment/Decrement
        if (allowIncrementDecrement && rand.Next(2) == 0)
        {
            shouldIncrementDecrement = true;
            if (allowExplicitTypecasting && rand.Next(2) == 0) { shouldTypecasting = true; }
        }
        else if (allowExplicitTypecasting && rand.Next(2) == 0)
        {
            shouldTypecasting = true;
            if (allowIncrementDecrement && rand.Next(2) == 0) { shouldIncrementDecrement = true; }
        }

        // Wenn Typecasting -> Sicherstellen, dass der neue Typ sich vom alten Typ unterscheidet
        if (shouldTypecasting)
        {
            do { castType1 = types[rand.Next(types.Count)]; }
            while (castType1 == var1Type);

            do { castType2 = types[rand.Next(types.Count)]; }
            while (castType2 == var2Type);
        }

        // Wenn Inkrement/Dekrement -> Berechnung des Ergebnisses und Generierung des Codes
        if (shouldIncrementDecrement)
        {
            if (position1.StartsWith("++")) var1Value++;
            if (position1.StartsWith("--")) var1Value--;
            if (position2.StartsWith("++")) var2Value++;
            if (position2.StartsWith("--")) var2Value--;
        }

        // Berechnung des Ergebnisses und Generierung des Codes
        if (castPosition == 0)
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = (int)var1Value + var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType1}){position1} + {position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting: {varType} {varName} = ({castType1}){position1} + {position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType1}){position1} + {position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting: {varName} = ({castType1}){position1} + {position2}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = (int)var1Value + var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType1}){var1Name} + {var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting: {varType} {varName} = ({castType1}){var1Name} + {var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType1}){var1Name} + {var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting: {varName} = ({castType1}){var1Name} + {var2Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value + var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} + {position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement: {varType} {varName} = {position1} + {position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} + {position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement: {varName} = {position1} + {position2}"); }
                }
            }
            else
            {
                result = var1Value + var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} + {var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition: {varType} {varName} = {var1Name} + {var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} + {var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition: {varName} = {var1Name} + {var2Name}"); }
                }
            }
        }
        else if (castPosition == 1)
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var2Type == "float") { result = var1Value + (int)var2Value; }         // Typecasting beachten -> var2Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} + ({castType2}){position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting: {varType} {varName} = {position1} + ({castType2}){position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} + ({castType2}){position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting: {varName} = {position1} + ({castType2}){position2}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var2Type == "float") { result = var1Value + (int)var2Value; }         // Typecasting beachten -> var2Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} + ({castType2}){var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting: {varType} {varName} = {var1Name} + ({castType2}){var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} + ({castType2}){var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting: {varName} = {var1Name} + ({castType2}){var2Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value + var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} + {position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement: {varType} {varName} = {position1} + {position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} + {position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement: {varName} = {position1} + {position2}"); }
                }
            }
            else
            {
                result = var1Value + var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} + {var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition: {varType} {varName} = {var1Name} + {var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} + {var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition: {varName} = {var1Name} + {var2Name}"); }
                }
            }
        }
        else
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float" && var2Type == "float") { result = (int)var1Value + (int)var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else if (var1Type == "float") { result = (int)var1Value + var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else if (var2Type == "float") { result = var1Value + (int)var2Value; } // Typecasting beachten -> var2Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType1}){position1} + ({castType2}){position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting: {varType} {varName} = ({castType1}){position1} + ({castType2}){position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType1}){position1} + ({castType2}){position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting: {varName} = ({castType1}){position1} + ({castType2}){position2}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float" && var2Type == "float") { result = (int)var1Value + (int)var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else if (var1Type == "float") { result = (int)var1Value + var2Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else if (var2Type == "float") { result = var1Value + (int)var2Value; } // Typecasting beachten -> var2Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + var2Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType1}){var1Name} + ({castType2}){var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting: {varType} {varName} = ({castType1}){var1Name} + ({castType2}){var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType1}){var1Name} + ({castType2}){var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting: {varName} = ({castType1}){var1Name} + ({castType2}){var2Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value + var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} + {position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement: {varType} {varName} = {position1} + {position2}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} + {position2};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement: {varName} = {position1} + {position2}"); }
                }
            }
            else
            {
                result = var1Value + var2Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} + {var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition: {varType} {varName} = {var1Name} + {var2Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} + {var2Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition: {varName} = {var1Name} + {var2Name}"); }
                }
            }
        }

        if (shouldIncrementDecrement)
        {
            if (position1.EndsWith("++")) var1Value++;
            if (position1.EndsWith("--")) var1Value--;
            if (position2.EndsWith("++")) var2Value++;
            if (position2.EndsWith("--")) var2Value--;
        }

        // Aktualisierung der Werte im ScopeStack
        result = (float)Math.Round(result, 2);
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        // Sonst -> Aktualisierung des Wertes im ScopeStack
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }

        // Inkrement/Dekrement -> Aktualisierung der Werte im ScopeStack
        if (shouldIncrementDecrement)
        {
            if (varName != var1Name) { UpdateVariableInScopeStack(var1Name, var1Value, var1Type, scopeStack); }
            if (varName != var2Name) { UpdateVariableInScopeStack(var2Name, var2Value, var2Type, scopeStack); }
        }
    }

    private void HandleOneVariableSubtraction(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, float var1Value, string var1Type, char var1Name, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        // Keine Char Operation mit var1 - randomValue oder randomValue - var1 möglich
        if (CharValueOutOfRange(varType, var1Value))
        {
            HandleTwoRandomNumbersSubtraction(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack);
            return;
        }

        float result, randomValue1;
        int castPosition = rand.Next(2);
        string castType = "";
        string operation1 = rand.Next(2) == 0 ? "++" : "--";
        string position1 = rand.Next(2) == 0 ? $"{operation1}{var1Name}" : $"{var1Name}{operation1}";
        bool shouldTypecasting = false;
        bool shouldIncrementDecrement = false;

        // Zufällige Entscheidung, ob Typecasting und/oder Increment/Decrement
        if (allowIncrementDecrement && rand.Next(2) == 0)
        {
            shouldIncrementDecrement = true;
            if (allowExplicitTypecasting && rand.Next(2) == 0)
            {
                shouldTypecasting = true;
            }
        }
        else if (allowExplicitTypecasting && rand.Next(2) == 0)
        {
            shouldTypecasting = true;
            if (allowIncrementDecrement && rand.Next(2) == 0)
            {
                shouldIncrementDecrement = true;
            }
        }

        // Wenn Typecasting -> Sicherstellen, dass der neue Typ sich vom alten Typ unterscheidet
        if (shouldTypecasting)
        {
            do
            {
                castType = types[rand.Next(types.Count)];
            } while (castType == var1Type);
        }

        // Generierung einer Zufallszahl
        if (varType == "char" && castPosition == 0)
        {
            do
            {
                if (rand.Next(2) == 0) { randomValue1 = GenerateRandomValue("float"); }
                else { randomValue1 = GenerateRandomValue("int"); }
            } while (var1Value - randomValue1 < 98 || var1Value - randomValue1 > 121);
        }
        else if (varType == "char" && castPosition == 1)
        {
            do
            {
                if (rand.Next(2) == 0) { randomValue1 = (float)(rand.NextDouble() * 1000.0 - 500.0); randomValue1 = (float)Math.Round(randomValue1, 2); }
                else { randomValue1 = rand.Next(-500, 501); }
            } while (randomValue1 - var1Value < 98 || randomValue1 - var1Value > 121);
        }
        else
        {
            if (rand.Next(2) == 0) { randomValue1 = GenerateRandomValue("float"); }
            else { randomValue1 = GenerateRandomValue("int"); }
        }

        // Wenn Inkrement/Dekrement -> Berechnung des Ergebnisses und Generierung des Codes
        if (shouldIncrementDecrement)
        {
            if (position1.StartsWith("++")) var1Value++;
            if (position1.StartsWith("--")) var1Value--;
        }

        // Berechnung des Ergebnisses und Generierung des Codes
        if (castPosition == 0)
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = (int)var1Value - randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value - randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType}){position1} - {randomValue1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting {varType} {varName} = ({castType}){position1} - {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType}){position1} - {randomValue1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting {varName} = ({castType}){position1} - {randomValue1}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = (int)var1Value - randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value - randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType}){var1Name} - {randomValue1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting {varType} {varName} = ({castType}){var1Name} - {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType}){var1Name} - {randomValue1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting: {varName} = ({castType}){var1Name} - {randomValue1}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value - randomValue1;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} - {randomValue1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement {varType} {varName} = {position1} - {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} - {randomValue1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement: {varName} = {position1} - {randomValue1}"); }
                }
            }
            else
            {
                result = var1Value - randomValue1;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} - {randomValue1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction {varType} {varName} = {var1Name} - {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} - {randomValue1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction: {varName} = {var1Name} - {randomValue1}"); }
                }
            }
        }
        else
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = randomValue1 - (int)var1Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = randomValue1 - var1Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} - ({castType}){position1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting {varType} {varName} = {randomValue1} - ({castType}){position1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} - ({castType}){position1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement with Typecasting: {varName} = {randomValue1} - ({castType}){position1}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = randomValue1 - (int)var1Value; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = randomValue1 - var1Value; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} - ({castType}){var1Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting {varType} {varName} = {randomValue1} - ({castType}){var1Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} - ({castType}){var1Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Typecasting: {varName} = {randomValue1} - ({castType}){var1Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = randomValue1 - var1Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} - {position1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement {varType} {varName} = {randomValue1} - {position1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} - {position1};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction with Increment/Decrement: {varName} = {randomValue1} - {position1}"); }
                }
            }
            else
            {
                result = randomValue1 - var1Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} - {var1Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction {varType} {varName} = {randomValue1} - {var1Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} - {var1Name};\\r\\n");
                    if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction: {varName} = {randomValue1} - {var1Name}"); }
                }
            }
        }

        if (shouldIncrementDecrement)
        {
            if (position1.EndsWith("++")) var1Value++;
            if (position1.EndsWith("--")) var1Value--;
        }

        // Aktualisierung der Werte im ScopeStack
        result = (float)Math.Round(result, 2);
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        // Sonst -> Aktualisierung des Wertes im ScopeStack
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }
        // Inkrement/Dekrement -> Aktualisierung der Werte im ScopeStack
        if (shouldIncrementDecrement && varName != var1Name) { UpdateVariableInScopeStack(var1Name, var1Value, var1Type, scopeStack); }
    }

    private void HandleOneVariableAddition(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, float var1Value, string var1Type, char var1Name, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        // Keine Char Operation mit var1 + randomValue möglich
        if (CharValueOutOfRange(varType, var1Value))
        {
            HandleTwoRandomNumbersAddition(isVariableDeclaration, code, indent, varName, varType, currentScope, types, scopeStack);
            return;
        }

        float result, randomValue1;
        int castPosition = rand.Next(2);
        string castType = "";
        string operation1 = rand.Next(2) == 0 ? "++" : "--";
        string position1 = rand.Next(2) == 0 ? $"{operation1}{var1Name}" : $"{var1Name}{operation1}";
        bool shouldTypecasting = false;
        bool shouldIncrementDecrement = false;

        // Zufällige Entscheidung, ob Typecasting und/oder Increment/Decrement
        if (allowIncrementDecrement && rand.Next(2) == 0)
        {
            shouldIncrementDecrement = true;
            if (allowExplicitTypecasting && rand.Next(2) == 0)
            {
                shouldTypecasting = true;
            }
        }
        else if (allowExplicitTypecasting && rand.Next(2) == 0)
        {
            shouldTypecasting = true;
            if (allowIncrementDecrement && rand.Next(2) == 0)
            {
                shouldIncrementDecrement = true;
            }
        }

        // Wenn Typecasting -> Sicherstellen, dass der neue Typ sich vom alten Typ unterscheidet
        if (shouldTypecasting)
        {
            do
            {
                castType = types[rand.Next(types.Count)];
            } while (castType == var1Type);
        }

        // Generierung einer Zufallszahl
        if (varType == "char")
        {
            do
            {
                if (rand.Next(2) == 0) { randomValue1 = GenerateRandomValue("float"); }
                else { randomValue1 = GenerateRandomValue("int"); }
            } while (var1Value + randomValue1 < 98 || var1Value + randomValue1 > 121);
        }
        else
        {
            if (rand.Next(2) == 0) { randomValue1 = GenerateRandomValue("float"); }
            else { randomValue1 = GenerateRandomValue("int"); }
        }

        // Wenn Inkrement/Dekrement -> Berechnung des Ergebnisses und Generierung des Codes
        if (shouldIncrementDecrement)
        {
            if (position1.StartsWith("++")) var1Value++;
            if (position1.StartsWith("--")) var1Value--;
        }

        // Berechnung des Ergebnisses und Generierung des Codes
        if (castPosition == 0)
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = (int)var1Value + randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType}){position1} + {randomValue1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting {varType} {varName} = ({castType}){position1} + {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType}){position1} + {randomValue1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting {varName} = ({castType}){position1} + {randomValue1}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = (int)var1Value + randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = ({castType}){var1Name} + {randomValue1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting {varType} {varName} = ({castType}){var1Name} + {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = ({castType}){var1Name} + {randomValue1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting: {varName} = ({castType}){var1Name} + {randomValue1}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = var1Value + randomValue1;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {position1} + {randomValue1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement {varType} {varName} = {position1} + {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {position1} + {randomValue1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement: {varName} = {position1} + {randomValue1}"); }
                }
            }
            else
            {
                result = var1Value + randomValue1;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {var1Name} + {randomValue1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition {varType} {varName} = {var1Name} + {randomValue1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {var1Name} + {randomValue1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition: {varName} = {var1Name} + {randomValue1}"); }
                }
            }
        }
        else
        {
            if (shouldTypecasting && shouldIncrementDecrement)
            {
                if (var1Type == "float") { result = (int)var1Value + randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} + ({castType}){position1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting {varType} {varName} = {randomValue1} + ({castType}){position1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} + ({castType}){position1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement with Typecasting: {varName} = {randomValue1} + ({castType}){position1}"); }
                }
            }
            else if (shouldTypecasting)
            {
                if (var1Type == "float") { result = (int)var1Value + randomValue1; } // Typecasting beachten -> var1Type != castType -> float nach ... -> Nachkommastellen gehen verloren
                else { result = var1Value + randomValue1; }

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} + ({castType}){var1Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting {varType} {varName} = {randomValue1} + ({castType}){var1Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} + ({castType}){var1Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Typecasting: {varName} = {randomValue1} + ({castType}){var1Name}"); }
                }
            }
            else if (shouldIncrementDecrement)
            {
                result = randomValue1 + var1Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} + {position1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement {varType} {varName} = {randomValue1} + {position1}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} + {position1};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition with Increment/Decrement: {varName} = {randomValue1} + {position1}"); }
                }
            }
            else
            {
                result = randomValue1 + var1Value;

                if (isVariableDeclaration)
                {
                    code.Append($"{indent}{varType} {varName} = {randomValue1} + {var1Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition {varType} {varName} = {randomValue1} + {var1Name}"); }
                }
                else
                {
                    code.Append($"{indent}{varName} = {randomValue1} + {var1Name};\\r\\n");
                    if (debug && debugAddition) { Console.WriteLine($"* Addition: {varName} = {randomValue1} + {var1Name}"); }
                }
            }
        }

        if (shouldIncrementDecrement)
        {
            if (position1.EndsWith("++")) var1Value++;
            if (position1.EndsWith("--")) var1Value--;
        }

        // Aktualisierung der Werte im ScopeStack
        result = (float)Math.Round(result, 2);
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        // Sonst -> Aktualisierung des Wertes im ScopeStack
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }
        // Inkrement/Dekrement -> Aktualisierung der Werte im ScopeStack
        if (shouldIncrementDecrement && varName != var1Name) { UpdateVariableInScopeStack(var1Name, var1Value, var1Type, scopeStack); }
    }

    private void HandleTwoRandomNumbersSubtraction(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        float result, randomValue1, randomValue2;

        // Generierung von zwei Zufallszahlen
        if (varType == "char")
        {
            do
            {
                if (rand.Next(3) == 0) { randomValue1 = GenerateRandomValue("float"); randomValue2 = GenerateRandomValue("int"); }
                else if (rand.Next(3) == 1) { randomValue1 = GenerateRandomValue("int"); randomValue2 = GenerateRandomValue("float"); }
                else { randomValue1 = GenerateRandomValue("int"); randomValue2 = GenerateRandomValue("int"); }
            } while (randomValue1 - randomValue2 < 97 || randomValue1 - randomValue2 > 122);
        }
        else
        {
            if (rand.Next(3) == 0) { randomValue1 = GenerateRandomValue("float"); randomValue2 = GenerateRandomValue("int"); }
            else if (rand.Next(3) == 1) { randomValue1 = GenerateRandomValue("int"); randomValue2 = GenerateRandomValue("float"); }
            else { randomValue1 = GenerateRandomValue("int"); randomValue2 = GenerateRandomValue("int"); }
        }

        // Berechnung des Ergebnisses
        result = randomValue1 - randomValue2;
        result = (float)Math.Round(result, 2);

        // Generierung des Codes
        if (isVariableDeclaration)
        {
            code.Append($"{indent}{varType} {varName} = {randomValue1} - {randomValue2};\\r\\n");
            if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction {varType} {varName} = {randomValue1} - {randomValue2}"); }
        }
        else
        {
            code.Append($"{indent}{varName} = {randomValue1} - {randomValue2};\\r\\n");
            if (debug && debugSubtraction) { Console.WriteLine($"* Subtraction: {varName} = {randomValue1} - {randomValue2}"); }
        }

        // Aktualisierung des Ergebnisses im ScopeStack
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }
    }

    private void HandleTwoRandomNumbersAddition(bool isVariableDeclaration, StringBuilder code, string indent, char varName, string varType, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        float result, randomValue1, randomValue2;

        // Generierung von zwei Zufallszahlen
        if (varType == "char")
        {
            do
            {
                if (rand.Next(3) == 0) { randomValue1 = GenerateRandomValue("float"); randomValue2 = GenerateRandomValue("int"); }
                else if (rand.Next(3) == 1) { randomValue1 = GenerateRandomValue("int"); randomValue2 = GenerateRandomValue("float"); }
                else { randomValue1 = GenerateRandomValue("int"); randomValue2 = GenerateRandomValue("int"); }
            } while (randomValue1 + randomValue2 < 97 || randomValue1 + randomValue2 > 122);
        }
        else
        {
            if (rand.Next(3) == 0) { randomValue1 = GenerateRandomValue("float"); randomValue2 = GenerateRandomValue("int"); }
            else if (rand.Next(3) == 1) { randomValue1 = GenerateRandomValue("int"); randomValue2 = GenerateRandomValue("float"); }
            else { randomValue1 = GenerateRandomValue("int"); randomValue2 = GenerateRandomValue("int"); }
        }

        // Berechnung des Ergebnisses
        result = randomValue1 + randomValue2;
        result = (float)Math.Round(result, 2);

        // Generierung des Codes
        if (isVariableDeclaration)
        {
            code.Append($"{indent}{varType} {varName} = {randomValue1} + {randomValue2};\\r\\n");
            if (debug && debugAddition) { Console.WriteLine($"* Addition {varType} {varName} = {randomValue1} + {randomValue2}"); }
        }
        else
        {
            code.Append($"{indent}{varName} = {randomValue1} + {randomValue2};\\r\\n");
            if (debug && debugAddition) { Console.WriteLine($"* Addition: {varName} = {randomValue1} + {randomValue2}"); }
        }

        // Aktualisierung des Ergebnisses im ScopeStack
        // Wenn Deklaration -> Hinzufügen der Variable zum aktuellen Scope
        if (isVariableDeclaration)
        {
            if (varType == "float")
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = result }); }
            }
            else
            {
                if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = (int)result }); }
            }
        }
        // Sonst -> Aktualisierung des Wertes im ScopeStack
        else
        {
            if (varType == "float") { UpdateVariableInScopeStack(varName, result, varType, scopeStack); }
            else { UpdateVariableInScopeStack(varName, (int)result, varType, scopeStack); }
        }
    }

    private bool CharValueOutOfRange(string varType, float var1Value)
    {
        // Keine Char Operation mit var1 möglich -> zwei Zufallszahlen
        if (varType == "char" && (var1Value > 220 || var1Value < -2))   // 221, weil maxChar = 121, minRandom -100; -3, weil minChar = 97, maxRandom = 100; +-1 wegen möglichen Inkrement/Decrement
        {
            return true;
        }
        return false;
    }

    private void HandleShadowVariable(StringBuilder code, string indent, Dictionary<char, VariableInfo> otherScopes, Dictionary<char, VariableInfo> currentScope, List<string> types, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        // Wenn keine Variable verfügbar ist -> Deklaration
        var validItems = otherScopes.Where(item => !currentScope.ContainsKey(item.Key)).ToList();
        if (validItems.Count == 0)
        {
            HandleNewVariable(code, indent, otherScopes, currentScope, types, scopeStack);
            return;
        }

        KeyValuePair<char, VariableInfo> selectedVar = validItems[rand.Next(validItems.Count)];

        char varName = selectedVar.Key;
        string varType;

        do { varType = types[rand.Next(types.Count)]; }
        while (varType == selectedVar.Value.Type);

        float value = GenerateRandomValue(varType);

        code.Append($"{indent}{varType} {varName} = {value};\\r\\n");

        if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = value }); }
    }

    private void HandleNewVariable(StringBuilder code, string indent, Dictionary<char, VariableInfo> otherScopes, Dictionary<char, VariableInfo> currentScope, List<string> type, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        bool isVariableDeclaration = true;

        bool[] operations = new bool[] {
            allowVariableDeclaration,
            allowAddition,
            allowSubtraction,
            allowMultiplication,
            allowDivision
        };

        List<int> possibleOperations = new List<int>();
        for (int j = 0; j < operations.Length; j++)
        {
            if (operations[j]) { possibleOperations.Add(j); }
        }

        int actionIndex = rand.Next(possibleOperations.Count);
        int action = possibleOperations[actionIndex];

        if (action == 0)
        {
            var localVar = GenerateRandomVariable(types, otherScopes, currentScope);
            string varType = localVar.Item1;
            char varName = localVar.Item2;
            float varValue = localVar.Item3;

            code.Append($"{indent}{varType} {varName} = {varValue};\\r\\n");
            if (!currentScope.ContainsKey(varName)) { currentScope.Add(varName, new VariableInfo { Type = varType, Value = varValue }); }
        }
        else if (action == 1) { HandleAdditionSubtraction(isVariableDeclaration, true, code, indent, otherScopes, currentScope, types, scopeStack); }
        else if (action == 2) { HandleAdditionSubtraction(isVariableDeclaration, false, code, indent, otherScopes, currentScope, types, scopeStack); }
        else if (action == 3) { HandleMultiplicationDivision(isVariableDeclaration, true, code, indent, otherScopes, currentScope, types, scopeStack); }
        else if (action == 4) { HandleMultiplicationDivision(isVariableDeclaration, false, code, indent, otherScopes, currentScope, types, scopeStack); }
    }

    private void HandleAssignment(StringBuilder code, string indent, Dictionary<char, VariableInfo> otherScopes, Dictionary<char, VariableInfo> currentScope, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        // Wenn keine Variable verfügbar ist -> Deklaration
        if (otherScopes.Count == 0 && currentScope.Count == 0)
        {
            HandleNewVariable(code, indent, otherScopes, currentScope, types, scopeStack);
            return;
        }

        // Versuche zuerst, eine Variable aus einem äußeren Scope zu wählen
        KeyValuePair<char, VariableInfo> selectedVar;
        if (otherScopes.Count > 0)
        {
            List<KeyValuePair<char, VariableInfo>> itemListOtherScope = otherScopes.ToList();
            selectedVar = itemListOtherScope[rand.Next(itemListOtherScope.Count)];
        }

        // Wenn keine verfügbar ist, wähle eine aus dem eigenen Scope
        else
        {
            List<KeyValuePair<char, VariableInfo>> itemListCurrentScope = currentScope.ToList();
            selectedVar = itemListCurrentScope[rand.Next(itemListCurrentScope.Count)];
        }

        char varName = selectedVar.Key;
        string varType = selectedVar.Value.Type;
        float varValue = selectedVar.Value.Value;

        // Überprüfen, ob die ausgewählte Variable eine Schattenvariable hat, wenn ja Typ übernehmen
        if (currentScope.ContainsKey(varName))
        {
            varType = currentScope[varName].Type;
        }

        // Generiere neuen Wert für die Variable
        float value;
        do
        {
            value = GenerateRandomValue(varType);
        } while (value == varValue);

        code.Append($"{indent}{varName} = {value};\\r\\n");

        UpdateVariableInScopeStack(varName, value, varType, scopeStack);
    }

    private void UpdateVariableInScopeStack(char varName, float value, string varType, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        // Aktualisiere den Wert der Variable im ScopeStack
        for (int i = scopeStack.Count - 1; i >= 0; i--)     // Wichtige Logik:
        {                                                   // Wenn Variable in currentScope -> currentScope = tiefste Ebene
            var scope = scopeStack[i];                      // Wenn Variable in otherScopes -> läuft von tieferen nach oben und aktualisiert den Wert
            if (scope.ContainsKey(varName))                 //      -> Wichtig für Schattenvariablen -> aktualisiert immer nur die Variable der tiefsten Ebene
            {
                scope[varName].Value = value;
                break;
            }
        }
    }

    private Tuple<string, char, float> GenerateRandomVariable(List<string> types, Dictionary<char, VariableInfo> otherScopes, Dictionary<char, VariableInfo> currentScope)
    {
        char varName;
        string type = types[rand.Next(types.Count)];
        float value = GenerateRandomValue(type);

        do
        {
            varName = (char)('a' + rand.Next(0, 26));
        } while (currentScope.ContainsKey(varName) || otherScopes.ContainsKey(varName));

        return Tuple.Create(type, varName, value);
    }

    private float GenerateRandomValue(string type)
    {
        float value;

        switch (type)
        {
            case "float":
                float randomFloat = (float)(rand.NextDouble() * 200.0 - 100.0);
                randomFloat = (float)Math.Round(randomFloat, 2);
                value = randomFloat;
                break;
            case "int":
                float randomInt = rand.Next(-100, 101);
                value = randomInt;
                break;
            case "short":
                float randomShort = rand.Next(-100, 101);
                value = randomShort;
                break;
            case "long":
                float randomLong = rand.Next(-100, 101);
                value = randomLong;
                break;
            case "char":
                float randomChar = rand.Next(97, 123);
                value = randomChar;
                break;
            default: // Sicherheitsfallback
                value = 0;
                break;
        }
        return value;
    }

    private void AddBlock(StringBuilder code, ref string indent, ref int currentDepth, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        code.Append(indent + "{\\r\\n");
        scopeStack.Add(new Dictionary<char, VariableInfo>());
        currentDepth++;
        indent += "    ";
    }

    private void CloseBlock(StringBuilder code, ref string indent, ref int currentDepth, List<Dictionary<char, VariableInfo>> scopeStack)
    {
        indent = indent.Substring(0, indent.Length - 4);
        code.Append(indent + "}\\r\\n");
        scopeStack.RemoveAt(currentDepth - 1);
        currentDepth--;
    }

    private void AddLabel(StringBuilder code, string indent, int labelNumber)
    {
        code.Append($"{indent}/* Label " + labelNumber + " */\\r\\n");
    }
}

public class Program
{
    static void Main()
    {

    }
}

/*
public class Program
{
    static void Main()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

        Settings settings = new Settings();
        DifficultySettings chosenSettings = settings.Easy;

        CodeGenerator generator = new CodeGenerator(chosenSettings);
        generator.GenerateCode();
    }
}
*/

/*  
Todo:
    - Multiplikation und Division mit zwei Variablen?
        -> Multiplikation vielleicht? Prüfen ob beide Variablen zweistellig oder weniger?
        -> Division wurde getestet -> der Fall das die Division funktoiniert ohne das die Aufgaben zu schwer werden ist zu gering...

    - +=, -=, *=, /= -> Fehlen

    - Aufbau der Blöcke (öffnen = schließen) könnte nach sehr vielen Aufgaben monoton wirken


Ideen:
    - Regel damit nich alle Operationen in einem Block gleich sind (bzw. zwei nacheinander?) -> steuern das die Operationen in etwa gleich oft vorkommen?
        -> nicht möglich da theoretisch nur eine oder zwei Operationen in einem Block vorkommen können

    - Regel damit Operationen nicht zweimal die gleiche Variable nacheinader nutzen
        -> Sorgt für Abwechslung, Lerneffekt für bspw. Inkrement/Decrement dann aber nochmal eine neue Zuweisung


Wenn Zeit übrig (nicht so wichtig):
    - Erweiterte Code-Validierung? -> Compiler zur Überprüfung schon vorhanden
*/