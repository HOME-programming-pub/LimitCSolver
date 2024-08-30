using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using ViewModel.JsonTemplate;
using LimitCSolver.MainApplication.ViewModel;

namespace LimitCSolver.ProtocolInputApplication.ViewModel;

public class MainWindowViewModel : ObservableObject
{

    private SolveTask _currentConfig = new("", "", false, new ProtocolViewModel());


    public SolveTask CurrentConfig
    {
        get => _currentConfig;
        set => SetProperty(ref _currentConfig, value);
    }

    public RelayCommand LoadTaskCommand => new(LoadTaskAction);

    private void LoadTaskAction()
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Json Files (.lct.json)|*.lct.json|Alle Dateien (*.*)|*.*";
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == false)
            return;

        var filePath = openFileDialog.FileName;

        if (!File.Exists(filePath))
        {
            MessageBox.Show("Datei wurde nicht gefunden!");
            return;
        }

        var c = File.ReadAllText(filePath);
        var newconfig = JsonConvert.DeserializeObject<SolveTask>(c);

        if (newconfig != null)
        {
            CurrentConfig = newconfig;
        }
    }

    public RelayCommand ExportProtCommand => new(ExportProtAction);

    private void ExportProtAction()
    {
        // Check for empty fields
        var ErrConf = false;
        foreach (var protokolEntry in CurrentConfig.Protokol.Entrys)
        {
            foreach (var varEntry in protokolEntry.VarEntrys)
            {
                if ((!CurrentConfig.NeedTypes || !string.IsNullOrWhiteSpace(varEntry.Type)) && !string.IsNullOrWhiteSpace(varEntry.Value)) 
                    continue;

                var res = MessageBox.Show("Mindestens ein Feld ist nicht ausgefüllt, trotzdem exportieren?", "leeres Feld", MessageBoxButton.YesNo);
                if (res != MessageBoxResult.Yes) 
                    return;

                ErrConf = true;
                break;
            }

            if (ErrConf)
            {
                break;
            }
        }

        var dialog = new SaveFileDialog();
        dialog.FileName = $"Protokoll_{CurrentConfig.Name}";
        dialog.DefaultExt = ".json";
        dialog.Filter = "Json Files (.lcp.json)|*.lcp.json|Alle Dateien (*.*)|*.*";

        bool? result = dialog.ShowDialog();

        if (result != true)
            return;

        string filename = dialog.FileName;

        using StreamWriter file = File.CreateText(filename);
        JsonSerializer serializer = new JsonSerializer();
        serializer.Serialize(file, CurrentConfig.Protokol);
    }
}