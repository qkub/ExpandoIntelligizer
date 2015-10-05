using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using System.Dynamic;

public class MethodFinder
{
    List<TypeDefinition> allTypes;
    public List<PropertyDefinition> MethodsToProcess = new List<PropertyDefinition>();

    public MethodFinder(List<TypeDefinition> allTypes)
    {
        this.allTypes = allTypes;
    }

    public void Execute()
    {
        foreach (var type in allTypes)
        {
            if (type.IsInterface)
            {
                continue;
            }
            if (type.IsEnum)
            {
                continue;
            }
            
            foreach (var method in type.Properties.Where(p=>p.GetMethod.IsStatic && p.GetMethod.ReturnType.GetType() is IDynamicMetaObjectProvider))
            {
                MethodsToProcess.Add(method);
            }
        }
    }
}