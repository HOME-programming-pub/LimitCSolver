namespace LimitCSolver.LimitCInterpreter.Memory;

public class TypedValue
{

    public TypedValue(string type, object? value)
    {
        Type = type;

        SetNewValue(value);

    }

    public string Type { get; set; }
    public object? Value { get; set; }

    public void SetNewValue(object? value)
    {

        var baseType = Type.Replace("*", ""); // TODO: May replace with regex


        if (value == null)
        {
            Value = null;
        }
        else if (value is int valInt)
        {

            switch (baseType)
            {
                case "void":
                    throw new NotSupportedException("Variablen können nicht vom Typ void sein");
                    break;
                case "char":
                    Value = (int)valInt;
                    break;
                case "short":
                    Value = (int)valInt;
                    break;
                case "int":
                    Value = valInt;
                    break;
                case "long":
                    Value = (int)valInt;
                    break;
                case "float":
                    Value = (double)valInt;
                    break;
                case "double":
                    Value = (double)valInt;
                    break;
                default:
                    throw new NotSupportedException($"Kein valider Typ erkannt: {baseType}");
                    break;
            };
        }
        else if (value is double valDouble)
        {
            switch (baseType)
            {
                case "void":
                    throw new NotSupportedException("Variablen können nicht vom Typ void sein");
                    break;
                case "char":
                    Value = (int)valDouble;
                    break;
                case "short":
                    Value = (int)valDouble;
                    break;
                case "int":
                    Value = (int)valDouble;
                    break;
                case "long":
                    Value = (int)valDouble;
                    break;
                case "float":
                    Value = valDouble;
                    break;
                case "double":
                    Value = valDouble;
                    break;
                default:
                    throw new NotSupportedException($"Kein valider Typ erkannt: {baseType}");
                    break;
            };
        }
        else if (value is char valChar)
        {
            switch (baseType)
            {
                case "void":
                    throw new NotSupportedException("Variablen können nicht vom Typ void sein");
                    break;
                case "char":
                    Value = (int)valChar;
                    break;
                case "short":
                    Value = (int)valChar;
                    break;
                case "int":
                    Value = (int)valChar;
                    break;
                case "long":
                    Value = (int)valChar;
                    break;
                case "float":
                    Value = (double)valChar;
                    break;
                case "double":
                    Value = (double)valChar;
                    break;
                default:
                    throw new NotSupportedException($"Kein valider Typ erkannt: {baseType}");
                    break;
            };
        }
        else if (value is string _)
        {
            // Char Array is not supported
            throw new ArgumentException("Char-Arrays werden nicht unterstützt!");
        }
        else
        {
            throw new ArgumentException($"Nicht Unterstützter ValueType: {value.GetType()}.");
        }
    }

    public override string ToString()
    {

        string s = $"{Type}, ";

        if (Value == null)
            return s;

        var baseType = Type.Replace("*", ""); // TODO: May replace with regex


        s += baseType switch
        {
            "void" => throw new NotSupportedException("Variablen können nicht vom Typ void sein"),
            "char" => ((int)(char)Value).ToString(), // print char as int
            "short" => ((int)Value).ToString(),
            "int" => ((int)Value).ToString(),
            "long" => ((int)Value).ToString(),
            "float" => ((double)Value).ToString("F2"),
            "double" => ((double)Value).ToString("F2"),
            _ => throw new NotSupportedException("Kein valider Typ erkannt")

        }; ;

        return s;
    }

}