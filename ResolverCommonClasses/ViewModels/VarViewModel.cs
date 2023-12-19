using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CommonClasses.ViewModels;

public class VarViewModel : ObservableValidator
{
    public VarViewModel(string name, string type, string value, string value2)
    {
        Name = name;
        Type = type;
        Value = value;
        SecondValue = value2;
    }

    private string _name = string.Empty;
    private string _type = string.Empty;
    private string _value = string.Empty;
    private bool? _typeCheck = null;
    private bool? _valueCheck = null;
    private bool _corrected = false;
    private bool _absCorrectedType;
    private bool _absCorrectedValue;
    private bool _gotPoint;
    private string _secondValue = string.Empty;
    private bool _failedToInclude;

    public int Index { get; set; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Type
    {
        get => _type;
        set
        {
            SetProperty(ref _type, value);
            ValidateAllProperties();
        }
    }

    [CustomValidation(typeof(VarViewModel), nameof(ValidateValue))]
    public string Value
    {
        get => _value;
        set
        {
            if (Type is "float" or "double")
                value = value.Replace(",", ".");

            SetProperty(ref _value, value, validate: true);
        }
    }

    public string SecondValue
    {
        get => _secondValue;
        set => SetProperty(ref _secondValue, value);
    }

    public bool? TypeCheck
    {
        get => _typeCheck;
        set => SetProperty(ref _typeCheck, value);
    }

    public bool? ValueCheck
    {
        get => _valueCheck;
        set => SetProperty(ref _valueCheck, value);
    }

    public bool Corrected
    {
        get => _corrected;
        set => SetProperty(ref _corrected, value);
    }

    public bool AbsCorrectedType
    {
        get => _absCorrectedType;
        set => SetProperty(ref _absCorrectedType, value);
    }

    public bool AbsCorrectedValue
    {
        get => _absCorrectedValue;
        set => SetProperty(ref _absCorrectedValue, value);
    }

    public bool FailedToInclude
    {
        get => _failedToInclude;
        set => SetProperty(ref _failedToInclude, value);
    }

    public bool GotPoint
    {
        get => _gotPoint;
        set => SetProperty(ref _gotPoint, value);
    }

    public static ValidationResult? ValidateValue(string value, ValidationContext context)
    {
        VarViewModel instance = (VarViewModel)context.ObjectInstance;
        
        if(string.IsNullOrWhiteSpace(instance.Type))
            return ValidationResult.Success;

        if (string.IsNullOrEmpty(value) || instance.Type.Contains('*'))
            return null;
        if (instance.Type is "short" or "int" or "long" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return null;
        }
        if (instance.Type is "float" or "double" && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return null;
        }
        if (instance.Type is "char" && (value.All(char.IsDigit) || CharRegex.IsMatch(value)))
        {
            return null;
        }
        
        return new($"Ungültige Eingabe für Typ {instance.Type}");
    }

    private static Regex CharRegex = new Regex("\'.{1}?\'");

}