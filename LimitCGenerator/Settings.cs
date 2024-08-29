using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LimitCSolver.LimitCGenerator;

public class DifficultySettings
{
    public int MinDepth { get; set; }
    public int MaxDepth { get; set; }
    public int MinVarCount { get; set; }
    public int MaxVarCount { get; set; }
    public int MinLabel { get; set; }
    public int MaxLabel { get; set; }
    public bool AllowGlobalVariables { get; set; }
    public bool AllowVariableDeclaration { get; set; }
    public bool AllowVariableAssignment { get; set; }
    public bool AllowShadowVariables { get; set; }
    public bool AllowAddition { get; set; }
    public bool AllowSubtraction { get; set; }
    public bool AllowMultiplication { get; set; }
    public bool AllowDivision { get; set; }
    public bool AllowIncrementDecrement { get; set; }
    public bool AllowExplicitTypecasting { get; set; }
}

public class Settings
{
    public DifficultySettings Easy { get; private set; }
    public DifficultySettings Medium { get; private set; }
    public DifficultySettings Hard { get; private set; }

    public Settings()
    {
        Easy = new DifficultySettings
        {
            MinDepth = 0,
            MaxDepth = 3,
            MinVarCount = 1,
            MaxVarCount = 2,
            MinLabel = 3,
            MaxLabel = 4,
            AllowGlobalVariables = true,
            AllowVariableDeclaration = true,
            AllowVariableAssignment = true,
            AllowShadowVariables = true,
            AllowAddition = false,
            AllowSubtraction = false,
            AllowMultiplication = false,
            AllowDivision = false,
            AllowIncrementDecrement = false,
            AllowExplicitTypecasting = false
        };
        Medium = new DifficultySettings
        {
            MinDepth = 2,
            MaxDepth = 3,
            MinVarCount = 1,
            MaxVarCount = 2,
            MinLabel = 5,
            MaxLabel = 6,
            AllowGlobalVariables = true,
            AllowVariableDeclaration = true,
            AllowVariableAssignment = true,
            AllowShadowVariables = true,
            AllowAddition = true,
            AllowSubtraction = true,
            AllowMultiplication = true,
            AllowDivision = true,
            AllowIncrementDecrement = false,
            AllowExplicitTypecasting = false
        };
        Hard = new DifficultySettings
        {
            MinDepth = 2,
            MaxDepth = 3,
            MinVarCount = 1,
            MaxVarCount = 2,
            MinLabel = 6,
            MaxLabel = 7,
            AllowGlobalVariables = true,
            AllowVariableDeclaration = true,
            AllowVariableAssignment = true,
            AllowShadowVariables = true,
            AllowAddition = true,
            AllowSubtraction = true,
            AllowMultiplication = true,
            AllowDivision = true,
            AllowIncrementDecrement = true,
            AllowExplicitTypecasting = true
        };
    }
}
