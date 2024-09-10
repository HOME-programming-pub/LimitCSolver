using LimitCSolver.LimitCInterpreter.Memory;

namespace LimitCSolver.LimitCInterpreter.SubTypes;

public class LabelCheckPointEventArgs : EventArgs
{
    public LabelCheckPointEventArgs(int labelNum, Dictionary<string, int> visibleVars, MemoryStorage memoryStorage)
    {
        LabelNum = labelNum;
        VisibleVars = visibleVars;
        MemoryStorage = memoryStorage;
    }

    public int LabelNum { get; set; } = 0;
    public Dictionary<string, int> VisibleVars { get; set; }
    public MemoryStorage MemoryStorage;


}