using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ViewModel;

namespace ViewModel.JsonTemplate;

public class ExtProtokolEntryViewModel : ObservableObject
{

    private int _label = 0; 
    private List<VarViewModel> _vars = new();

    public int Label
    {
        get => _label;
        set
        {
            _label = value;
            SetProperty(ref _label, value);
        }
    }

    public List<VarViewModel> Vars
    {
        get => _vars;
        set
        {
            _vars = value;
            SetProperty(ref _vars, value);
        }
    }

}