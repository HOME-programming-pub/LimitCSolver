using CommunityToolkit.Mvvm.ComponentModel;
using CommonClasses.ViewModels;

namespace CommonClasses.JsonTemplate;

public class SolveTask : ObservableObject
{
    public SolveTask(string code, string name, bool needTypes, ProtokolViewModel protokol)
    {
        _code = code;
        _name = name;
        _needTypes = needTypes;
        _protokol = protokol;
    }

    private string _code;
    private string _name;
    private bool _needTypes;
    private ProtokolViewModel _protokol;
    private decimal _pointForMatch = 0.5m;

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool NeedTypes
    {
        get => _needTypes;
        set => SetProperty(ref _needTypes, value);
    }

    public decimal PointForMatch
    {
        get => _pointForMatch;
        set => SetProperty(ref _pointForMatch, value);
    }

    public ProtokolViewModel Protokol
    {
        get => _protokol;
        set => SetProperty(ref _protokol, value);
    }
}