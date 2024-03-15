using Antlr4.Runtime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LimitCSolver;
using LimitCSolver.Memory;
using LimitCSolver.SubTypes;
using Microsoft.Win32;
using Newtonsoft.Json;
using CommonClasses.JsonTemplate;
using CommonClasses.ViewModels;
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

namespace ProtokollResolver.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{

    public MainWindowViewModel()
    {

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

    private void handleConfigInternalChange(object? sender, PropertyChangedEventArgs eas) 
    {
        CheckGivenProtokolActionCommand.NotifyCanExecuteChanged();
        CalcNewSolutionActionCommand.NotifyCanExecuteChanged();
        SyncProtocolCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckGivenProtokolActionCommand))]
    private ProtokolViewModel? _givenProtokol;
 

    [ObservableProperty]
    private ProtokolViewModel? _calcedSolution;

    [ObservableProperty]
    private Visibility _solutionVisibility = Visibility.Hidden;

    public ObservableCollection<(string varName, int varAddr)> CorrectedVars { get; set; } = new();


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
        if(GivenProtokol == null)
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
            //CalculateSolution();
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
        
        for(int i = 0; i < newProtocol.Entrys.Count && i < currentProtocol.Entrys.Count; i++)
        {
            var newEntry = newProtocol.Entrys[i];
            var currentEntry = currentProtocol.Entrys[i];
            foreach(var newVarEntry in newEntry.VarEntrys)
            {
                foreach(var currentVarEntry in currentEntry.VarEntrys)
                {
                    if(string.Equals(currentVarEntry.Name, newVarEntry.Name))
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
            CalcNewSolutionAction();
        CheckProtocol();
    }

    [RelayCommand(CanExecute = nameof(CanCalculateSolution))]
    private void CalcNewSolutionAction()
    {
        if(SolutionVisibility == Visibility.Hidden)
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
            Error($"Label {e.LabelNum} in Überprüfung erreicht, aber kein Protokoll zum Abgleich geladen?");
            return;
        }

        // ValueTypes vor Zeigern checken
        var vars = e.VisibleVars.OrderBy(se => e.MemoryStorage.Memory[se.Value].Type.Contains('*'));

        // Eintrag aus dem zu prüfenden Protokoll
        var protokollEntry = GivenProtokol.Entrys.FirstOrDefault(pe => pe.Num == e.LabelNum);
        if (protokollEntry == null)
        {
            Error($"Label {e.LabelNum} in Überprüfung erreicht, aber im Protokoll scheint kein entsprechender Eintrag vorhanden zu sein!");
            return;
        }

        // Eintrag aus der berechneten Lösung
        var absSltV = CalcedSolution?.Entrys.FirstOrDefault(pe => pe.Num == e.LabelNum);

        foreach (var (name, addr) in vars)
        {

            TypedValue memVal = e.MemoryStorage.Memory[addr];
            var p = new string('*', memVal.Type.Count(c => c == '*'));

            var absVarVal = absSltV?.VarEntrys.First(x => x.Name == $"{p}{name}").Value ?? "";
            var absVarType = absSltV?.VarEntrys.First(x => x.Name == $"{p}{name}").Type ?? "";

            var protVar = protokollEntry.VarEntrys.FirstOrDefault(pv => pv.Name == $"{p}{name}");

            if (protVar == null)
            {
                Error($"Fehler bei Label {e.LabelNum}, die sichtbare Variable {name} scheint" +
                      $" im Protokoll an der entsprechenden Stelle nicht definiert zu sein!");
                return;
            }

            protVar.GotPoint = false;

            if (!CurrentConfig.NeedTypes)
            {
                protVar.TypeCheck = true;
            }
            else
            {
                // Typed needed
                protVar.TypeCheck = memVal.Type == protVar.Type;
                if (protVar.TypeCheck == false)
                {
                    // Type war nicht der den der aktuelle Durchlauf berechnet hat (könnte bereits korrigiert sein)
                    protVar.TypeCheck = absVarType == protVar.Type;
                    if (protVar.TypeCheck == true)
                    {
                        // korrigiert berechnete Lösung Falsch, aber korrekt zur absoluten Lösung
                        protVar.AbsCorrectedType = true;
                    }
                }
            }

            var memV = memVal.Value; // Variablen Wert oder Ziel-Adresse (wenn Zeiger)
            var memA = -1;
            if (memVal.Type.Contains('*')) // aktuelle Variable ein Zeiger? -> memV == Adresse ?? null
            {
                if (memV != null) // kein Null-Pointer -> memV == Adresse
                {
                    TypedValue? vov = null;
                    if (e.MemoryStorage.Memory.ContainsKey((int)memV)) // Adresse definiert?
                    {
                        vov = e.MemoryStorage.Memory[(int)memV]; // Lookup Adresse im Speicher
                        memA = (int)memV;                       // Zeilwert oder neue Adresse
                        for (int i = 1; i < p.Length; i++) // Vorgang für Zeigertiefe {p.Length} wiederholen
                        {
                            // Abbruch, bei vorzeitigem Null-Zeiger oder nicht auflösbarem Ziel.
                            if (vov.Value == null || !e.MemoryStorage.Memory.ContainsKey((int)vov.Value))
                            {
                                vov = null;
                                break;
                            }
                            memA = (int)vov.Value;
                            vov = e.MemoryStorage.Memory[(int)vov.Value];
                        }
                    }
                    memV = vov?.Value ?? null; // zuletzt gefundenen Wert übernehmen
                }
            }

            string valval = "";
            string valval2 = "";
            if (memV is int memValInt)
            {
                valval = memValInt.ToString();
                if (memVal.Type.Contains("char"))
                    valval2 = ((char)memValInt).ToString();
            }
            else if (memV is double memValDouble)
            {
                valval = memValDouble.ToString("F2", CultureInfo.InvariantCulture);
            }
            else if (memV is null)
            {
                valval = "NULL";
            }

            protVar.ValueCheck = valval == protVar.Value || (!string.IsNullOrWhiteSpace(valval2) && valval2 == protVar.Value.Replace("'", ""));
            if (protVar.ValueCheck == false)
            {
                // Prot wert ist mindestens zu berechnetem Wert Falsch (könnte bereits korrigiert sein)
                protVar.ValueCheck = absVarVal == protVar.Value;
                if (protVar.ValueCheck == true)
                {
                    // Lösung war falsch zur relativen berechneten, aber jetzt wieder korrekt zur Absoluten Lösung
                    protVar.AbsCorrectedValue = true;
                }
            }

            // Anapssen der Fehlerhaften Variable im Speicher, damit bei der Weiterberechnung versucht wird die Folgefehler korrekt zu verarbeiten
            // Nur wenn Typefail oder Valuefail ODER wenn rückkehr zu originaler Lösung erkannt wurde
            if (protVar.TypeCheck == false || protVar.ValueCheck == false || protVar.AbsCorrectedType == true || protVar.AbsCorrectedValue == true)
            {
                object? nval = memV;
                string ntype = memVal.Type;

                // Wenn keiner der validen Typen gegeben ist -> Memory-Datatype beibehalten
                if (protVar.TypeCheck == false && protVar.Type is "int" or "short" or "long" or "float" or "double" or "char")
                {
                    ntype = protVar.Type;
                }

                try
                {
                    if (protVar.ValueCheck == false)
                    {
                        /*
                         * Typed Value erwartet ein Object, das allerdings innerhalb bereits correcten Types ist, weil in typed Value nur gecastet und nicht geparst wird!
                         * Also muss vorher in Abhängigkeit des Types ein Parsing erfolgen -> könnte auch failen
                         */

                        if (ntype is "int" or "short" or "long")
                        {
                            nval = int.Parse(protVar.Value);
                        }
                        else if (ntype is "float" or "double")
                        {
                            nval = double.Parse(protVar.Value, CultureInfo.InvariantCulture);
                        }
                        else if (ntype is "char")
                        {
                            int tci;
                            if (int.TryParse(protVar.Value, out tci))
                            {
                                // numerische Darstellung
                                nval = (char)tci;
                            }
                            else
                            {
                                nval = Convert.ToChar(protVar.Value.Replace("'", ""));
                            }

                        }
                    }

                    e.MemoryStorage.Memory[addr] = new TypedValue(ntype, nval);
                    CorrectedVars.Add((name, addr));
                }
                catch (Exception exception)
                {
                    Error(exception.Message);
                    protVar.FailedToInclude = true;
                }

                // Punktevergabe bei Rückkehr zur OriginalLösung
                if (protVar.TypeCheck == true && protVar.ValueCheck == true && (protVar.AbsCorrectedType == protVar.AbsCorrectedValue || (!CurrentConfig.NeedTypes && protVar.AbsCorrectedValue)))
                {
                    GivenProtokol.Points += CurrentConfig.PointForMatch;
                    protVar.GotPoint = true;
                }

                //if (p.Length > 0)
                //{
                //    if (CorrectedVars.Any(x => x.varAddr == memA))
                //        protVar.Corrected = true;
                //}

                if (CorrectedVars.Any(x => x.varAddr == addr))
                    protVar.Corrected = true;

            }
            else
            {

                //if (CorrectedVars.Contains((protVar.Name, addr)))
                if (CorrectedVars.Any(x => x.varAddr == addr || x.varAddr == memA) || valval != absVarVal || memVal.Type != absVarType)
                    protVar.Corrected = true;


                GivenProtokol.Points += CurrentConfig.PointForMatch;
                protVar.GotPoint = true;
            }
        }
    }

    private void VisitorOnLabelCheckPointReachedCreateSolution(object? sender, LabelCheckPointEventArgs e)
    {

        if (CalcedSolution == null)
            CalcedSolution = new();

        var vars = e.VisibleVars;

        var npe = new ProtokolEntryViewModel();
        npe.Num = e.LabelNum;

        foreach (var (name, addr) in vars)
        {
            if (!e.MemoryStorage.Memory.ContainsKey(addr))
            {
                Error($"Das hätte nicht passieren dürfen! Es wurde eine Adresse übergeben, welche keinen entsprechenden Speichereintrag besitzt! varName: {name} varAddr: {addr}");
                return;
            }

            TypedValue memVal = e.MemoryStorage.Memory[addr];

            var memV = memVal.Value;

            var p = new string('*', memVal.Type.Count(c => c == '*'));
            if (memVal.Type.Contains('*'))
            {
                if (memV != null)
                {
                    TypedValue? vov = null;
                    if (e.MemoryStorage.Memory.ContainsKey((int)memV))
                    {
                        vov = e.MemoryStorage.Memory[(int)memV];
                        for (int i = 1; i < p.Length; i++) // Vorgang für Zeigertiefe {p.Length} wiederholen
                        {
                            if (vov.Value == null || !e.MemoryStorage.Memory.ContainsKey((int)vov.Value))
                            {
                                vov = null;
                                break;
                            }
                            vov = e.MemoryStorage.Memory[(int)vov.Value];
                        }

                    }

                    memV = vov?.Value ?? null;
                }

            }

            string valval = "";
            string valval2 = "";
            if (memV is int memValInt)
            {
                valval = memValInt.ToString();
                if (memVal.Type.Contains("char"))
                {
                    valval2 = ((char)memValInt).ToString();
                }
            }
            else if (memV is double memValDouble)
            {
                valval = memValDouble.ToString("F2", CultureInfo.InvariantCulture);
            }
            else if (memV is null)
            {
                valval = "NULL";
            }

            npe.VarEntrys.Add(new VarViewModel($"{p}{name}", memVal.Type, valval, valval2));


        }

        CalcedSolution.Entrys.Add(npe);

    }

    public static void Error(string str)
    {
        MessageBox.Show(str);
    }

}