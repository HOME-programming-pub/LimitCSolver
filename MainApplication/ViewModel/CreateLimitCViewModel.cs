using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Windows.Input;
using System.Windows;
using LimitCSolver.LimitCGenerator;

namespace LimitCSolver.MainApplication.ViewModel;

public partial class CreateLimitCViewModel : ObservableObject
{

    public Settings Settings { get; private set; }
    private DifficultySettings _selectedDifficulty;

    [ObservableProperty]
    private Visibility _taskPopupVisibility;

    [ObservableProperty]
    private int _selectedTaskDifficultyindex;

    
    [ObservableProperty]
    private int _labeleMinVal;
    partial void OnLabeleMinValChanging(int value)
    {
        if (value > LabelMaxVal) { LabelMaxVal = value; }
    }

    [ObservableProperty]
    private int _labelMaxVal;
    partial void OnLabelMaxValChanging(int value)
    {
        if (value < LabeleMinVal) { LabeleMinVal = value; }
    }

    [ObservableProperty]
    private int _minDepthVal;
    partial void OnMinDepthValChanging(int value)
    {
        if (value > MaxDepthVal) { MaxDepthVal = value; }
    }

    [ObservableProperty]
    private int _maxDepthVal;
    partial void OnMaxDepthValChanging(int value)
    {
        if (value < MinDepthVal) { MinDepthVal = value; }
    }
    

    [ObservableProperty]
    private bool _isAllowGlobalVariables;

    [ObservableProperty]
    private bool _isAllowVariableDeclaration;

    [ObservableProperty]
    private bool _isAllowVariableAssignment;

    [ObservableProperty]
    private bool _isAllowShadowVariables;

    [ObservableProperty]
    private bool _isAllowAddition;

    [ObservableProperty]
    private bool _isAllowSubtraction;

    [ObservableProperty]
    private bool _isAllowMultiplication;

    [ObservableProperty]
    private bool _isAllowDivision;

    [ObservableProperty]
    private bool _isAllowIncrementDecrement;

    [ObservableProperty]
    private bool _isAllowExplicitTypecasting;

    public ICommand CmdSaveTaskSettings { get; }
    public ICommand CmdCloseTaskPopup { get; set; }

    public event EventHandler CloseTaskPopupRequested;

    public CreateLimitCViewModel()
    {
        Settings = new Settings();
        SelectedTaskDifficultyindex = 0;
        OnSelectedTaskDifficultyindexChanging(0);

        CmdSaveTaskSettings = new RelayCommand(FnSaveTaskSettings);
        CmdCloseTaskPopup = new RelayCommand(() => CloseTaskPopupRequested?.Invoke(this, EventArgs.Empty));
    }

    public void FnSaveTaskSettings()
    {
        _selectedDifficulty.MinLabel = LabeleMinVal;
        _selectedDifficulty.MaxLabel = LabelMaxVal;
        _selectedDifficulty.MinDepth = MinDepthVal;
        _selectedDifficulty.MaxDepth = MaxDepthVal;
        _selectedDifficulty.AllowGlobalVariables = IsAllowGlobalVariables;
        _selectedDifficulty.AllowVariableAssignment = IsAllowVariableAssignment;
        _selectedDifficulty.AllowVariableDeclaration = IsAllowVariableDeclaration;
        _selectedDifficulty.AllowShadowVariables = IsAllowShadowVariables;
        _selectedDifficulty.AllowAddition = IsAllowAddition;
        _selectedDifficulty.AllowSubtraction = IsAllowSubtraction;
        _selectedDifficulty.AllowMultiplication = IsAllowMultiplication;
        _selectedDifficulty.AllowDivision = IsAllowDivision;
        _selectedDifficulty.AllowIncrementDecrement = IsAllowIncrementDecrement;
        _selectedDifficulty.AllowExplicitTypecasting = IsAllowExplicitTypecasting;
        CmdCloseTaskPopup.Execute(null);
    }

    public void ExecuteGenerateCode()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
        CodeGenerator generator = new CodeGenerator(_selectedDifficulty);
        generator.GenerateCode();
    }

    partial void OnSelectedTaskDifficultyindexChanging(int value)
    {
        FnTaskChanged(value);
    }

    private void FnTaskChanged(int selectedTaskDifficultyindex)
    {
        switch (selectedTaskDifficultyindex)
        {
            case 0:
                _selectedDifficulty = Settings.Easy;
                break;
            case 1:
                _selectedDifficulty = Settings.Medium;
                break;
            case 2:
                _selectedDifficulty = Settings.Hard;
                break;
            default:
                _selectedDifficulty = Settings.Easy;
                break;
        }
        fnSetupDifficulty();
    }

    private void fnSetupDifficulty()
    {
        LabeleMinVal = _selectedDifficulty.MinLabel;
        LabelMaxVal = _selectedDifficulty.MaxLabel;
        MinDepthVal = _selectedDifficulty.MinDepth;
        MaxDepthVal = _selectedDifficulty.MaxDepth;
        IsAllowGlobalVariables = _selectedDifficulty.AllowGlobalVariables;
        IsAllowVariableAssignment = _selectedDifficulty.AllowVariableAssignment;
        IsAllowVariableDeclaration = _selectedDifficulty.AllowVariableDeclaration;
        IsAllowShadowVariables = _selectedDifficulty.AllowShadowVariables;
        IsAllowAddition = _selectedDifficulty.AllowAddition;
        IsAllowSubtraction = _selectedDifficulty.AllowSubtraction;
        IsAllowMultiplication = _selectedDifficulty.AllowMultiplication;
        IsAllowDivision = _selectedDifficulty.AllowDivision;
        IsAllowIncrementDecrement = _selectedDifficulty.AllowIncrementDecrement;
        IsAllowExplicitTypecasting = _selectedDifficulty.AllowExplicitTypecasting;
    }
}