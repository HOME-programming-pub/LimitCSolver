using System.Runtime.CompilerServices;

namespace LimitCSolver.LimitCInterpreter.SubTypes;

public class AddrValue
{

    public AddrValue(int addr)
    {
        Address = addr;
    }

    public int Address { get; set; }


}