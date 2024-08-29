using Antlr4.Runtime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Interpreter;
using Interpreter.Memory;
using Interpreter.SubTypes;
using Microsoft.Win32;
using Newtonsoft.Json;
using ViewModel.JsonTemplate;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Windows.Input;

namespace ViewModel;


public partial class MainWindowViewModel : ObservableObject
{
    #region Properties
    private SolveTask _currentConfig = new("", "", false, new ProtokolViewModel());
    public SolveTask CurrentConfig
    {
        get => _currentConfig;
        set
        {
            if (_currentConfig != null)
                _currentConfig.PropertyChanged -= handleConfigInternalChange;
            value.PropertyChanged += handleConfigInternalChange;
            SetProperty(ref _currentConfig, value);
            CheckGivenProtokolActionCommand.NotifyCanExecuteChanged();
            CalcNewSolutionActionCommand.NotifyCanExecuteChanged();
            SyncProtocolCommand.NotifyCanExecuteChanged();

        }
    }

    public CreateLimitCViewModel CreateLimitCVM { get; private set; }

    public ICommand CmdCloseTaskPopup { get; set; }

    [ObservableProperty]
    private Visibility _taskPopupVisibility = Visibility.Collapsed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckGivenProtokolActionCommand))]
    private ProtokolViewModel? _givenProtokol;

    [ObservableProperty]
    private ProtokolViewModel? _calcedSolution;

    [ObservableProperty]
    private Visibility _solutionVisibility = Visibility.Hidden;

    public ObservableCollection<(string varName, int varAddr)> CorrectedVars { get; set; } = new();
    #endregion

    #region Button Commands
    public RelayCommand GenerateCodeCommand { get; }
    public RelayCommand CmdSaveTask { get; }
    public RelayCommand CmdLoadTask { get; }
    public RelayCommand CmdLoadProtocol { get; }
    public RelayCommand CmdSaveProtocol { get; }
    #endregion

    public MainWindowViewModel()
    {
        CreateLimitCVM = new CreateLimitCViewModel();

        CreateLimitCVM.TaskPopupVisibility = this.TaskPopupVisibility;
        this.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(TaskPopupVisibility))
            {
                CreateLimitCVM.TaskPopupVisibility = TaskPopupVisibility;
            }
        };

        CmdCloseTaskPopup = new RelayCommand(FnCloseTaskPopup);
        CreateLimitCVM.CmdCloseTaskPopup = new RelayCommand(FnCloseTaskPopup);

        GenerateCodeCommand = new RelayCommand(ExecuteGenerateCode);

        CmdSaveTask = new RelayCommand(FndSaveTask);
        CmdLoadTask = new RelayCommand(FnLoadTask);
        CmdLoadProtocol = new RelayCommand(FnLoadProtocol);
        CmdSaveProtocol = new RelayCommand(FnSaveProtocol);
        TaskPopupVisibility = Visibility.Collapsed;


        //var p = new ProtokolViewModel();

        //var pe1 = new ProtokolEntryViewModel();
        //pe1.Num = 1;
        //pe1.VarEntrys.Add(new VarViewModel("a", "double", "0.00"));
        //pe1.VarEntrys.Add(new VarViewModel("b", "", "1"));
        //pe1.VarEntrys.Add(new VarViewModel("c", "", "2"));
        //p.Entrys.Add(pe1);

        //var pe2 = new ProtokolEntryViewModel();
        //pe2.Num = 2;
        //pe2.VarEntrys.Add(new VarViewModel("a", "", "78"));
        //pe2.VarEntrys.Add(new VarViewModel("b", "", "1"));
        //pe2.VarEntrys.Add(new VarViewModel("c", "", "3"));
        //p.Entrys.Add(pe2);

        //var pe3 = new ProtokolEntryViewModel();
        //pe3.Num = 3;
        //pe3.VarEntrys.Add(new VarViewModel("a", "", "5"));
        //pe3.VarEntrys.Add(new VarViewModel("b", "long", "-42"));
        //pe3.VarEntrys.Add(new VarViewModel("c", "", "3"));
        //p.Entrys.Add(pe3);

        //var pe4 = new ProtokolEntryViewModel();
        //pe4.Num = 4;
        //pe4.VarEntrys.Add(new VarViewModel("a", "", "5"));
        //pe4.VarEntrys.Add(new VarViewModel("b", "", "1"));
        //pe4.VarEntrys.Add(new VarViewModel("c", "", "3"));
        //p.Entrys.Add(pe4);

        //var pe5 = new ProtokolEntryViewModel();
        //pe5.Num = 5;
        //pe5.VarEntrys.Add(new VarViewModel("a", "", "5"));
        //pe5.VarEntrys.Add(new VarViewModel("b", "", "1"));
        //pe5.VarEntrys.Add(new VarViewModel("c", "", "3"));
        //p.Entrys.Add(pe5);

        //var pe6 = new ProtokolEntryViewModel();
        //pe6.Num = 6;
        //pe6.VarEntrys.Add(new VarViewModel("a", "", "0.00"));
        //pe6.VarEntrys.Add(new VarViewModel("b", "", "1"));
        //pe6.VarEntrys.Add(new VarViewModel("c", "", "3"));
        //pe6.VarEntrys.Add(new VarViewModel("d", "", "8"));
        //p.Entrys.Add(pe6);

        //GivenProtokol = p;



    }

    private void FnCloseTaskPopup()
    {
        TaskPopupVisibility = TaskPopupVisibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
        CreateLimitCVM.TaskPopupVisibility = TaskPopupVisibility;
    }

    private void ExecuteGenerateCode()
    {
        CreateLimitCVM.ExecuteGenerateCode();

        string filePath = "..\\..\\..\\..\\_Modul_CreateLimitC\\Model\\output.lct.json";

        if (!File.Exists(filePath))
        {
            MessageBox.Show("Datei wurde nicht gefunden: " + filePath);
            return;
        }

        LoadTaskFromFile(filePath);
        SyncProtocolCommand.Execute(null);
    }

    private void FnSaveProtocol()
    {
        if (HasEmptyFields() == true)
        {
            var res = MessageBox.Show("Mindestens ein Feld ist nicht ausgefüllt, trotzdem exportieren?", "leeres Feld", MessageBoxButton.YesNo);
            if (res != MessageBoxResult.Yes)
                return;
        }

        var dialog = new SaveFileDialog();
        dialog.FileName = $"Protokoll_{CurrentConfig.Name}";
        dialog.DefaultExt = ".json";
        dialog.Filter = "Json Files (.lcp.json)|*.lcp.json|Alle Dateien (*.*)|*.*";

        bool? result = dialog.ShowDialog();

        if (result != true)
            return;

        string filename = dialog.FileName;

        SaveProtocolToFile(filename);
    }

    private void FnLoadProtocol()
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Json Files (.lcp.json)|*.lcp.json|Alle Dateien (*.*)|*.*";
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() == false)
            return;

        var filePath = openFileDialog.FileName;

        if (!File.Exists(filePath))
        {
            MessageBox.Show("Datei wurde nicht gefunden!");
            return;
        }

        LoadProtocolFromFile(filePath);
    }

    private void FnLoadTask()
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

        LoadTaskFromFile(filePath);
    }

    private void FndSaveTask()
    {
        if (string.IsNullOrWhiteSpace(CurrentConfig.Code))
        {
            Error("Kein Programm vorhanden!");
            return;
        }

        try
        {
            var dialog = new SaveFileDialog();
            dialog.FileName = $"Aufgabenstellung_{CurrentConfig.Name.Replace(" ", "_")}";
            dialog.DefaultExt = ".json";
            dialog.Filter = "Json Files (.lct.json)|*.lct.json|Alle Dateien (*.*)|*.*";

            bool? result = dialog.ShowDialog();

            if (result != true)
                return;

            string filename = dialog.FileName;

            SaveTaskToFile(filename);

        }
        catch (Exception e)
        {
            MessageBox.Show("Eine Exception ist aufgetreten");
            MessageBox.Show(e.Message);
            Console.WriteLine(e);
        }
    }

    private void Error(string str)
    {
        MessageBox.Show(str);
    }

    private void handleConfigInternalChange(object? sender, PropertyChangedEventArgs eas)
    {
        CheckGivenProtokolActionCommand.NotifyCanExecuteChanged();
        CalcNewSolutionActionCommand.NotifyCanExecuteChanged();
        SyncProtocolCommand.NotifyCanExecuteChanged();
    }

    private static LimitCParser.ProgContext parse(string code)
    {
        AntlrInputStream inputStream = new AntlrInputStream(code);
        LimitCLexer limitCLexer = new LimitCLexer(inputStream);
        CommonTokenStream commonTokenStream = new CommonTokenStream(limitCLexer);
        LimitCParser limitCParser = new LimitCParser(commonTokenStream);

        //limitCParser.ErrorHandler = new DefaultErrorStrategy(){}
        limitCParser.RemoveErrorListeners();
        limitCParser.AddErrorListener(new DiagnosticErrorListener());

        //limitCParser.AddErrorListener();

        var limitCContext = limitCParser.prog();

        return limitCContext;
    }

    private bool CanCheckProtocol() => GivenProtokol != null && !string.IsNullOrWhiteSpace(CurrentConfig.Code);

    /// <summary>
    /// Check a given protocol.
    /// </summary>
    private void CheckProtocol()
    {
        if (GivenProtokol == null)
            return;
        GivenProtokol.ClearAllChecks();
        GivenProtokol.Points = 0;
        CorrectedVars.Clear();

        var program = parse(CurrentConfig.Code);
        var interpreter = new LimitCInterpreter();

        interpreter.LabelCheckPointReached += VisitorOnLabelCheckPointReachedCheckProtokol;
        interpreter.evaluate(program);
    }

    private bool CanCalculateSolution => !string.IsNullOrWhiteSpace(CurrentConfig.Code);

    /// <summary>
    /// Calculate the correct solution.
    /// </summary>
    private void CalculateSolution()
    {
        if (string.IsNullOrWhiteSpace(CurrentConfig.Code))
        {
            return;
        }
        CalcedSolution = new ProtokolViewModel();
        var program = parse(CurrentConfig.Code);
        var interpreter = new LimitCInterpreter();

        interpreter.LabelCheckPointReached += VisitorOnLabelCheckPointReachedCreateSolution;
        interpreter.evaluate(program);
    }

    public bool HasEmptyFields()
    {
        // Check for empty fields
        var ErrConf = false;
        foreach (var protokolEntry in CurrentConfig.Protokol.Entrys)
        {
            foreach (var varEntry in protokolEntry.VarEntrys)
            {
                if ((!CurrentConfig.NeedTypes || !string.IsNullOrWhiteSpace(varEntry.Type)) && !string.IsNullOrWhiteSpace(varEntry.Value))
                    continue;

                ErrConf = true;
                break;
            }

            if (ErrConf)
            {
                break;
            }
        }
        return ErrConf;
    }

    public void SaveProtocolToFile(string filename)
    {
        using StreamWriter file = File.CreateText(filename);
        JsonSerializer serializer = new JsonSerializer();
        serializer.Serialize(file, CurrentConfig.Protokol);
    }


    public void LoadProtocolFromFile(string path)
    {
        var fileContents = File.ReadAllText(path);
        ProtokolViewModel newprot = new ProtokolViewModel();

        try
        {
            newprot = JsonConvert.DeserializeObject<ProtokolViewModel>(fileContents) ?? new ProtokolViewModel();
        }
        catch (Exception e)
        {
            // Kein Protokoll aus Input Tool?
            //var t = JArray.Parse(c);
            var t = JsonConvert.DeserializeObject<List<ExtProtokolEntryViewModel>>(fileContents) ?? new List<ExtProtokolEntryViewModel>();
            foreach (var model in t)
            {
                newprot.Entrys.Add(new ProtokolEntryViewModel(model.Label, model.Vars));
            }
        }

        newprot.Points = 0;
        GivenProtokol = newprot;
    }

    public void LoadTaskFromFile(string path)
    {
        var contents = File.ReadAllText(path);
        var newconfig = JsonConvert.DeserializeObject<SolveTask>(contents);

        if (newconfig != null)
        {
            CurrentConfig = newconfig;
            CalcedSolution = new ProtokolViewModel();
            CalculateSolution();
        }
    }

    public void SaveTaskToFile(string path)
    {
        var newProtocol = ExtractEmptyProtocolFromProgram(CurrentConfig.Code);
        var newTask = new SolveTask(CurrentConfig.Code, CurrentConfig.Name, CurrentConfig.NeedTypes, newProtocol);

        using StreamWriter file = File.CreateText(path);
        JsonSerializer serializer = new JsonSerializer();
        serializer.Serialize(file, newTask);
    }

    private ProtokolViewModel ExtractEmptyProtocolFromProgram(string code)
    {
        var newProtocol = new ProtokolViewModel();
        var interpreter = new LimitCInterpreter();
        var program = parse(CurrentConfig.Code);

        interpreter.LabelCheckPointReached += (sender, args) =>
        {
            var npe = new ProtokolEntryViewModel() { Num = args.LabelNum };

            foreach (var (name, addr) in args.VisibleVars)
            {
                TypedValue memVal = args.MemoryStorage.Memory[addr];
                var p = new string('*', memVal.Type.Count(c => c == '*'));

                npe.VarEntrys.Add(new VarViewModel($"{p}{name}", "", "", ""));
            }
            newProtocol.Entrys.Add(npe);
        };

        interpreter.evaluate(program);

        return newProtocol;
    }

    [RelayCommand(CanExecute = nameof(CanCalculateSolution))]
    private void SyncProtocol()
    {
        var newProtocol = ExtractEmptyProtocolFromProgram(CurrentConfig.Code);
        var currentProtocol = GivenProtokol;
        if (currentProtocol == null)
        {
            GivenProtokol = newProtocol;
            return;
        }

        for (int i = 0; i < newProtocol.Entrys.Count && i < currentProtocol.Entrys.Count; i++)
        {
            var newEntry = newProtocol.Entrys[i];
            var currentEntry = currentProtocol.Entrys[i];
            foreach (var newVarEntry in newEntry.VarEntrys)
            {
                foreach (var currentVarEntry in currentEntry.VarEntrys)
                {
                    if (string.Equals(currentVarEntry.Name, newVarEntry.Name))
                    {
                        newVarEntry.Value = currentVarEntry.Value;
                        newVarEntry.Type = currentVarEntry.Type;
                        break;
                    }
                }
            }
        }
        GivenProtokol = newProtocol;
    }

    [RelayCommand(CanExecute = nameof(CanCheckProtocol))]
    private void CheckGivenProtokolAction()
    {
        if (CalcedSolution == null)
            CalculateSolution();
        CheckProtocol();
    }

    [RelayCommand(CanExecute = nameof(CanCalculateSolution))]
    private void CalcNewSolutionAction()
    {
        if (SolutionVisibility == Visibility.Hidden)
        {
            CalculateSolution();
            SolutionVisibility = Visibility.Visible;
            OnPropertyChanged(nameof(CalcedSolution));
        }
        else
        {
            SolutionVisibility = Visibility.Hidden;
        }

    }

    private void VisitorOnLabelCheckPointReachedCheckProtokol(object? sender, LabelCheckPointEventArgs e)
    {

        // Prüfung ob Protokoll geladen
        if (GivenProtokol == null)
        {
            return;
        }

        // ValueTypes vor Zeigern checken
        var visibleVars = e.VisibleVars.OrderBy(se => e.MemoryStorage.Memory[se.Value].Type.Contains('*'));

        // Eintrag aus dem zu prüfenden Protokoll
        var protocolEntry = GivenProtokol.Entrys.FirstOrDefault(pe => pe.Num == e.LabelNum);
        if (protocolEntry == null)
        {
            GivenProtokol.ProtocolOrLabelMismatchMessage = $"Label {e.LabelNum} in Überprüfung erreicht, aber im Protokoll scheint kein entsprechender Eintrag vorhanden zu sein!";
            GivenProtokol.ProtocolLabelOrVarMismatch = true;
            return;
        }

        // Eintrag aus der berechneten Lösung
        var calculatedEntry = CalcedSolution?.Entrys.FirstOrDefault(pe => pe.Num == e.LabelNum);

        foreach (var (name, addr) in visibleVars)
        {
            TypedValue currentMemoryCell = e.MemoryStorage.Memory[addr];

            var indirectionCount = currentMemoryCell.Type.Count(c => c == '*');
            var pointerChain = new string('*', indirectionCount);

            var calculatedValue = calculatedEntry?.VarEntrys.First(x => x.Name == $"{pointerChain}{name}").Value ?? "";
            var calculatedType = calculatedEntry?.VarEntrys.First(x => x.Name == $"{pointerChain}{name}").Type ?? "";

            var protocolVariable = protocolEntry.VarEntrys.FirstOrDefault(pv => pv.Name == $"{pointerChain}{name}");

            if (protocolVariable == null)
            {
                GivenProtokol.ProtocolOrLabelMismatchMessage = $"Fehler bei Label {e.LabelNum}, die sichtbare Variable {name} scheint" +
                      $" im Protokoll an der entsprechenden Stelle nicht definiert zu sein!";
                GivenProtokol.ProtocolLabelOrVarMismatch = true;
                return;
            }



            protocolVariable.clearCheckState();

            if (!CurrentConfig.NeedTypes)
            {
                protocolVariable.TypeCheck = true;
            }
            else
            {
                // Typed needed
                protocolVariable.TypeCheck = currentMemoryCell.Type == protocolVariable.Type;
                if (protocolVariable.TypeCheck == false)
                {
                    // Type war nicht der den der aktuelle Durchlauf berechnet hat (könnte bereits korrigiert sein)
                    protocolVariable.TypeCheck = calculatedType == protocolVariable.Type;
                    if (protocolVariable.TypeCheck == true)
                    {
                        // korrigiert berechnete Lösung Falsch, aber korrekt zur absoluten Lösung
                        protocolVariable.AbsCorrectedType = true;
                    }
                }
            }

            var memoryValue = currentMemoryCell.Value; // Variablen Wert oder Ziel-Adresse (wenn Zeiger)
            var addressValue = -1;

            if (currentMemoryCell.Type.Contains('*')) // aktuelle Variable ein Zeiger? -> memoryValue == Adresse ?? null
            {
                if (memoryValue != null) // kein Null-Pointer -> memoryValue == Adresse
                {
                    TypedValue? nextMemoryCell = null;
                    if (e.MemoryStorage.Memory.ContainsKey((int)memoryValue)) // Adresse definiert?
                    {
                        addressValue = (int)memoryValue;
                        nextMemoryCell = e.MemoryStorage.Memory[addressValue]; // Lookup Adresse im Speicher
                                                                               // Zeilwert oder neue Adresse
                        for (int i = 1; i < pointerChain.Length; i++) // Vorgang für Zeigertiefe {p.Length} wiederholen
                        {
                            // Abbruch, bei vorzeitigem Null-Zeiger oder nicht auflösbarem Ziel.
                            if (nextMemoryCell.Value == null || !e.MemoryStorage.Memory.ContainsKey((int)nextMemoryCell.Value))
                            {
                                nextMemoryCell = null;
                                break;
                            }
                            addressValue = (int)nextMemoryCell.Value;
                            nextMemoryCell = e.MemoryStorage.Memory[(int)nextMemoryCell.Value];
                        }
                    }
                    memoryValue = nextMemoryCell?.Value ?? null; // zuletzt gefundenen Wert übernehmen
                }
            }

            string stringMemoryValue = "";
            string alternativeRepresentation = "";
            if (memoryValue is int intMemoryValue)
            {
                stringMemoryValue = intMemoryValue.ToString();
                if (currentMemoryCell.Type.Contains("char"))
                    alternativeRepresentation = ((char)intMemoryValue).ToString();
            }
            else if (memoryValue is double doubleMemoryValue)
            {
                stringMemoryValue = doubleMemoryValue.ToString("F2", CultureInfo.InvariantCulture);
            }
            else if (memoryValue is null)
            {
                stringMemoryValue = "NULL";
            }

            protocolVariable.ValueCheck = stringMemoryValue == protocolVariable.Value || (!string.IsNullOrWhiteSpace(alternativeRepresentation) && alternativeRepresentation == protocolVariable.Value.Replace("'", ""));
            if (protocolVariable.ValueCheck == false)
            {
                // Prot wert ist mindestens zu berechnetem Wert Falsch (könnte bereits korrigiert sein)
                protocolVariable.ValueCheck = calculatedValue == protocolVariable.Value;
                if (protocolVariable.ValueCheck == true)
                {
                    // Lösung war falsch zur relativen berechneten, aber jetzt wieder korrekt zur Absoluten Lösung
                    protocolVariable.AbsCorrectedValue = true;
                }
            }

            // Anpassen der Fehlerhaften Variable im Speicher, damit bei der Weiterberechnung versucht wird die Folgefehler korrekt zu verarbeiten
            // Nur wenn Typefail oder Valuefail ODER wenn rückkehr zu originaler Lösung erkannt wurde
            if (protocolVariable.TypeCheck == false
                || protocolVariable.ValueCheck == false
                || protocolVariable.AbsCorrectedType == true
                || protocolVariable.AbsCorrectedValue == true)
            {
                object? replacedMemoryValue = memoryValue;
                string replacedMemoryType = currentMemoryCell.Type;

                // Wenn keiner der validen Typen gegeben ist -> Memory-Datatype beibehalten 
                if (protocolVariable.TypeCheck == false && protocolVariable.Type is "int" or "short" or "long" or "float" or "double" or "char")
                {
                    replacedMemoryType = protocolVariable.Type; //!TODO: Check, this is not the memory data type as stated above
                }

                try
                {
                    if (protocolVariable.ValueCheck == false)
                    {
                        /*
                         * Typed Value erwartet ein Object, das allerdings innerhalb bereits correcten Types ist, weil in typed Value nur gecastet und nicht geparst wird!
                         * Also muss vorher in Abhängigkeit des Types ein Parsing erfolgen -> könnte auch failen
                         */

                        if (replacedMemoryType is "int" or "short" or "long")
                        {
                            replacedMemoryValue = int.Parse(protocolVariable.Value);
                        }
                        else if (replacedMemoryType is "float" or "double")
                        {
                            replacedMemoryValue = double.Parse(protocolVariable.Value, CultureInfo.InvariantCulture);
                        }
                        else if (replacedMemoryType is "char")
                        {
                            int temp;
                            if (int.TryParse(protocolVariable.Value, out temp))
                            {
                                // numerische Darstellung
                                replacedMemoryValue = (char)temp;
                            }
                            else
                            {
                                //!TODO Check this conversion
                                replacedMemoryValue = Convert.ToChar(protocolVariable.Value.Replace("'", ""));
                            }

                        }
                    }

                    e.MemoryStorage.Memory[addr] = new TypedValue(replacedMemoryType, replacedMemoryValue);
                    CorrectedVars.Add((name, addr));
                }
                catch (Exception exception)
                {
                    protocolVariable.FailedToIncludeMessage = exception.Message;
                    protocolVariable.FailedToInclude = true;
                }

                // Punktevergabe bei Rückkehr zur OriginalLösung
                if (protocolVariable.TypeCheck == true && protocolVariable.ValueCheck == true && (protocolVariable.AbsCorrectedType == protocolVariable.AbsCorrectedValue || (!CurrentConfig.NeedTypes && protocolVariable.AbsCorrectedValue)))
                {
                    GivenProtokol.Points += CurrentConfig.PointForMatch;
                    protocolVariable.GotPoint = true;
                }

                //if (p.Length > 0)
                //{
                //    if (CorrectedVars.Any(x => x.varAddr == memA))
                //        protVar.Corrected = true;
                //}

                if (CorrectedVars.Any(x => x.varAddr == addr))
                    protocolVariable.Corrected = true;

            }
            else
            {

                //if (CorrectedVars.Contains((protVar.Name, addr)))
                if (CorrectedVars.Any(x => x.varAddr == addr || x.varAddr == addressValue) || stringMemoryValue != calculatedValue || currentMemoryCell.Type != calculatedType)
                    protocolVariable.Corrected = true;


                GivenProtokol.Points += CurrentConfig.PointForMatch;
                protocolVariable.GotPoint = true;
            }
        }
    }

    private void VisitorOnLabelCheckPointReachedCreateSolution(object? sender, LabelCheckPointEventArgs e)
    {

        if (CalcedSolution == null)
            CalcedSolution = new();

        var vars = e.VisibleVars;

        var newProtocolEntry = new ProtokolEntryViewModel();
        newProtocolEntry.Num = e.LabelNum;

        foreach (var (name, addr) in vars)
        {
            if (!e.MemoryStorage.Memory.ContainsKey(addr))
            {
                throw new($"Das hätte nicht passieren dürfen! Es wurde eine Adresse übergeben, welche keinen entsprechenden Speichereintrag besitzt! varName: {name} varAddr: {addr}");
            }

            TypedValue typedValue = e.MemoryStorage.Memory[addr];

            var valueString = typedValue.Value;

            var indirectionsCount = typedValue.Type.Count(c => c == '*');

            if (typedValue.Type.Contains('*'))
            {
                if (valueString != null)
                {
                    TypedValue? nextValue = null;
                    if (e.MemoryStorage.Memory.ContainsKey((int)valueString))
                    {
                        nextValue = e.MemoryStorage.Memory[(int)valueString];
                        for (int i = 1; i < indirectionsCount; i++) // Vorgang für Zeigertiefe {p.Length} wiederholen
                        {
                            if (nextValue.Value == null || !e.MemoryStorage.Memory.ContainsKey((int)nextValue.Value))
                            {
                                nextValue = null;
                                break;
                            }
                            nextValue = e.MemoryStorage.Memory[(int)nextValue.Value];
                        }

                    }

                    valueString = nextValue?.Value ?? null;
                }

            }

            string protocolValue = "";
            string alternativeRepresentation = "";
            if (valueString is int intValue)
            {
                protocolValue = intValue.ToString();
                if (typedValue.Type.Contains("char"))
                {
                    alternativeRepresentation = ((char)intValue).ToString();
                }
            }
            else if (valueString is double doubleValue)
            {
                protocolValue = doubleValue.ToString("F2", CultureInfo.InvariantCulture);
            }
            else if (valueString is null)
            {
                protocolValue = "NULL";
            }

            var pointerChain = new string('*', indirectionsCount);
            newProtocolEntry.VarEntrys.Add(new VarViewModel($"{pointerChain}{name}", typedValue.Type, protocolValue, alternativeRepresentation));
        }

        CalcedSolution.Entrys.Add(newProtocolEntry);

    }

}