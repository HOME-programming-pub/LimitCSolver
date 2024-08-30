using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ViewModel;

namespace ViewModel.JsonTemplate;

public class ExtProtokolViewModel : ObservableObject
{
    private List<ExtProtokolEntryViewModel> _entrys = new();

    public List<ExtProtokolEntryViewModel> Entrys
    {
        get => _entrys;
        set => SetProperty(ref _entrys, value);
    }
}