using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LimitCSolver.MainApplication.ViewModel;

public partial class ProtokolViewModel : ObservableObject
{
    public ProtokolViewModel()
    {
        Entrys.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(MaxVarCount));
    }

    private ObservableCollection<ProtokolEntryViewModel> _entrys = new();
    private decimal? _points = null;

    [ObservableProperty]
    private bool _protocolLabelOrVarMismatch = false;
    [ObservableProperty]
    private string _protocolOrLabelMismatchMessage = String.Empty;

    public ObservableCollection<ProtokolEntryViewModel> Entrys
    {
        get => _entrys;
        set => SetProperty(ref _entrys, value);
    }

    public int MaxVarCount
    {
        get
        {
            int max = 0;
            foreach (var entry in Entrys)
            {
                if(entry.VarEntrys.Count > max)
                    max = entry.VarEntrys.Count;
            }

            return max;
        }
    }

    public decimal? Points
    {
        get => _points;
        set => SetProperty(ref _points, value);
    }

    public void ClearAllChecks()
    {
        foreach (var protokolEntryViewModel in Entrys)
        {
            foreach (var varViewModel in protokolEntryViewModel.VarEntrys)
            {
                varViewModel.TypeCheck = null;
                varViewModel.ValueCheck = null;
            }
        }

        Points = null;
    }
}