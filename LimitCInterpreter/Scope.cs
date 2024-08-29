namespace LimitCSolver.LimitCInterpreter;

public class Scope
{
    public Scope() { }

    public Dictionary<string, int> vars = new();

    public Stack<Scope> SubScopes = new();

    public void AddSubScope()
    {
        SubScopes.Push(new Scope());
    }

    public void RemoveSubScope()
    {
        SubScopes.Pop();
    }

    public void AddVar(string name, int addr)
    {
        if (SubScopes.Count > 0)
        {
            var cs = SubScopes.Peek();
            cs.AddVar(name, addr);
            return;
        }

        if (vars.ContainsKey(name))
        {
            throw new InvalidOperationException($"var {name} is always defined in the current Scope!");
        }

        vars.Add(name, addr);

    }

    public bool ContainsVar(string varName)
    {
        foreach (Scope scope in SubScopes)
        {
            if (scope.ContainsVar(varName))
            {
                return true;
            }
        }

        if (vars.ContainsKey(varName))
        {
            return true;
        }

        return false;
    }

    public int GetAdress(string varName)
    {
        foreach (Scope scope in SubScopes)
        {
            var addr = scope.GetAdress(varName);
            if (addr != -1 )
            {
                return addr;
            }
        }

        if (vars.ContainsKey(varName))
        {
            return vars[varName];
        }

        return -1;

    }


}