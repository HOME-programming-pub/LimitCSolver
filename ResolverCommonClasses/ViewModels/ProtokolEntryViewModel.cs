using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommonClasses.ViewModels;

public class ProtokolEntryViewModel : ObservableObject
{
    public ProtokolEntryViewModel()
    {
        _varEntrys.CollectionChanged += (sender, args) => Renumerate();
    }

    public ProtokolEntryViewModel(int labelNum, IEnumerable<VarViewModel> vars)
    {
        _num = labelNum;
        VarEntrys = new ObservableCollection<VarViewModel>(vars);

        _varEntrys.CollectionChanged += (sender, args) => Renumerate();
    }



    private int _num = 0; 
    private ObservableCollection<VarViewModel> _varEntrys = new();

    public int Num
    {
        get => _num;
        set
        {
            _num = value;
            SetProperty(ref _num, value);
        }
    }

    public ObservableCollection<VarViewModel> VarEntrys
    {
        get => _varEntrys;
        set
        {
            _varEntrys = value;
            SetProperty(ref _varEntrys, value);
            Renumerate();
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        Renumerate();
    }

    public void Renumerate()
    {
        for (int i = 0; i < VarEntrys.Count; i++)
        {
            VarEntrys[i].Index = i;
        }
    }

}