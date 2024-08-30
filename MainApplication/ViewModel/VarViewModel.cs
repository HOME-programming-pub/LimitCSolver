using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LimitCSolver.MainApplication.ViewModel;

public partial class VarViewModel : ObservableValidator
{
    public VarViewModel(string name, string type, string value, string valueRepresentation)
    {
        Name = name;
        Type = type;
        Value = value;
        ValueRepresentation = valueRepresentation;
    }

    // Protocol input values
    private string _name = string.Empty;
    private string _type = string.Empty;
    private string _value = string.Empty;
    private string _valueRepresentation = string.Empty;

    // Protocol check state
    private bool? _typeCheck = null;
    private bool? _valueCheck = null;
    private bool _corrected = false;
    private bool _absCorrectedType = false;
    private bool _absCorrectedValue = false;
    private bool _gotPoint = false;
    private bool _failedToInclude = false;
    [ObservableProperty]
    private string _failedToIncludeMessage = String.Empty;

    public void clearCheckState()
    {
        TypeCheck = null;
        ValueCheck = null;
        Corrected = false;
        AbsCorrectedType = false;
        AbsCorrectedValue = false;
        GotPoint = false;
        FailedToInclude = false;
        FailedToIncludeMessage = String.Empty;
    }

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

    /// <summary>
    /// An alternative representation of the actual value in the protocol. Currently used to store rendered characters.
    /// </summary>
    public string ValueRepresentation
    {
        get => _valueRepresentation;
        set => SetProperty(ref _valueRepresentation, value);
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