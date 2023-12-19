using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommonClasses.ViewModels;

namespace CommonClasses.JsonTemplate;

public class ExtProtokolViewModel : ObservableObject
{
    private List<ExtProtokolEntryViewModel> _entrys = new();

    public List<ExtProtokolEntryViewModel> Entrys
    {
        get => _entrys;
        set => SetProperty(ref _entrys, value);
    }
}