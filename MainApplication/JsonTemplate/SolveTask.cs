﻿using CommunityToolkit.Mvvm.ComponentModel;
using LimitCSolver.MainApplication.ViewModel;

namespace ViewModel.JsonTemplate;

public partial class SolveTask : ObservableObject
{
    public SolveTask(string code, string name, bool needTypes, ProtocolViewModel protokol)
    {
        _code = code;
        _name = name;
        _needTypes = needTypes;
        _protokol = protokol;
    }

    [ObservableProperty]
    private string _code;
    
    [ObservableProperty]
    private string _name;
    
    [ObservableProperty]
    private bool _needTypes;
    
    [ObservableProperty]
    private ProtocolViewModel _protokol;
    
    [ObservableProperty]
    private decimal _pointForMatch = 0.5m;
}