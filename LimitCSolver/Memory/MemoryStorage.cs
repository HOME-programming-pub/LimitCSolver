namespace LimitCSolver.Memory;

public class MemoryStorage
{
    public int MemoryLastPos = 0;
    public Dictionary<int, TypedValue> Memory = new();

    public int AddToMemory(string type, object? val)
    {
        var pos = MemoryLastPos++;
        Memory.Add(pos, new(type, val));
        return pos;
    }

    public object? GetFromMemory(int addr)
    {

        if (!Memory.ContainsKey(addr))
        {
            throw new InvalidOperationException($"No var at address {addr} found!");
        }

        return Memory[addr].Value;
    }

    public void ChangeInMemory(int addr, object? nval)
    {
        if (!Memory.ContainsKey(addr))
        {
            throw new InvalidOperationException($"No var at address {addr} found!");
        }

        Memory[addr].SetNewValue(nval);

    }

}