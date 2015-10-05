using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;
using AssemblyToProcess;
using System.Collections.Generic;
using System.Linq;
using AssemblyToProcessExternal;

[TestFixture]
public class WeaverTests
{
    Assembly assembly;
    string newAssemblyPath;
    string shadowAssemblyPath;
    string assemblyPath;

    [TestFixtureSetUp]
    public void Setup()
    {
        var projectPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\AssemblyToProcess\AssemblyToProcess.csproj"));
        assemblyPath = Path.Combine(Path.GetDirectoryName(projectPath), @"bin\Debug\AssemblyToProcess.dll");
#if (!DEBUG)
        assemblyPath = assemblyPath.Replace("Debug", "Release");
#endif

        newAssemblyPath = assemblyPath.Replace(".dll", "2.dll");
        shadowAssemblyPath = assemblyPath.Replace(".dll", "2shadow.dll");
        File.Copy(assemblyPath, newAssemblyPath, true);
        File.Copy(assemblyPath, shadowAssemblyPath, true);
        
        var moduleDefinition = ModuleDefinition.ReadModule(newAssemblyPath);
        var weavingTask = new ModuleWeaver
        {
            ModuleDefinition = moduleDefinition            
        };

        weavingTask.Execute();

        moduleDefinition.Write(newAssemblyPath);

        assembly = Assembly.LoadFile(newAssemblyPath);
    }

    [Test]
    public void ValidateDictMemberToProperty()
    {
        var types = assembly.GetTypes();
        var type = types.Where(t => t.Name == "SeedRepository").FirstOrDefault();
        
        var ntype = Activator.CreateInstance(type);

        var prop = type.GetProperty("NamedList1");
        var result = prop.GetValue(ntype, null);        
        Assert.NotNull(result);

        var prop2 = type.GetProperty("Perestroika");
        var result2 = prop2.GetValue(ntype, null);
        Assert.NotNull(result2);
        Assert.IsAssignableFrom(typeof(Perestroika),result2);
    }

    [Test]
    public void TestExpandoInitialized()
    {
        var intialRepo = (typeof(AssemblyToProcess.SEREPO.SeedRepository)).Assembly.GetType("AssemblyToProcess.SEREPO.SeedRepository");
        var repoType = assembly.GetType("AssemblyToProcess.SEREPO.SeedRepository");
        
        Assert.NotNull(intialRepo);
        Assert.NotNull(repoType);

        var stringProp = repoType.GetProperty("NamedList1"); 
        Assert.NotNull(stringProp);
        var newPropExtractTypeFullName = String.Format("{0}.{1}.{2}",repoType.Namespace,repoType.Name, "NamedList1");
        var type = repoType.Assembly.GetType(newPropExtractTypeFullName);
        var objProp = repoType.GetProperty("SimpleObject"); 
        Assert.NotNull(objProp);

        var objPerestroika = repoType.GetProperty("Perestroika");
        Assert.NotNull(objPerestroika);
    }

#if(DEBUG)
    [Test]
    public void PeVerify()
    {
        Verifier.Verify(assemblyPath,newAssemblyPath);
    }
#endif
}