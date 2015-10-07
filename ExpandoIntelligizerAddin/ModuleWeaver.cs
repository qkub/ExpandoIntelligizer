using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

public class ModuleWeaver
{
    // Will log an informational message to MSBuild
    public Action<string, SequencePoint> LogErrorPoint;
    public Action<string, SequencePoint> LogInfoPoint;
    public Action<string> LogInfo { get; set; }
    public Action<string> LogWarning { get; set; }

    public static string IFACEName = "ISeedSource";
    // An instance of Mono.Cecil.ModuleDefinition for processing
    public ModuleDefinition ModuleDefinition { get; set; }

    public string Config { get; set; }
    public string AddInDirectoryPath { get; set; }
    public string AssemblyFilePath { get; set; }
    public string AssemblyResolver { get; set; }        
    public string References { get; set; }
    public List<string> ReferenceCopyLocalPaths { get; set; }
    public string ProjectDirectoryPath { get; set; }
    public string SolutionDirectoryPath { get; set; }
    public string DefineConstants { get; set; }
    

    // Init logging delegates to make testing easier
    public ModuleWeaver()
    {
        LogInfo = s => { };
        LogErrorPoint = (s, p) => { };
        LogWarning = s => { };
    }

    public void Execute()
    {
        var msCoreReferenceFinder = new MsCoreReferenceFinder(this, ModuleDefinition.AssemblyResolver);
        msCoreReferenceFinder.Execute();

        var allTypes = ModuleDefinition.GetTypes()
                        .Where(t => t.Interfaces
                                        .Any(itr=>itr.Name==IFACEName))
                        .ToList();
        
        var fieldToPropertyConverter = new FieldToPropertyConverter(this, msCoreReferenceFinder, ModuleDefinition.TypeSystem, allTypes,ModuleDefinition);
        fieldToPropertyConverter.Execute();
        
    }

   
}