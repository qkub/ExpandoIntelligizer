using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Dynamic;
using System.Linq;
using System;
using System.IO;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using rf=System.Reflection;

public class FieldToPropertyConverter
{

    MsCoreReferenceFinder msCoreReferenceFinder;
    TypeSystem typeSystem;
    List<TypeDefinition> allTypes;    
    ModuleDefinition MainModuleDef;
    ModuleWeaver moduleWeaver;

    public static string DICT_DATA_EXTRACTION_PROP_NAME = "Data";

    public FieldToPropertyConverter(ModuleWeaver moduleWeaver, MsCoreReferenceFinder msCoreReferenceFinder,
                                    TypeSystem typeSystem, List<TypeDefinition> allTypes,ModuleDefinition moduleDef)
    {
        this.moduleWeaver = moduleWeaver;
        this.msCoreReferenceFinder = msCoreReferenceFinder;
        this.typeSystem = typeSystem;
        this.allTypes = allTypes;        
        this.MainModuleDef = moduleDef;
    }

    void Process(TypeDefinition typeDefinition)
    {
        var prop = typeDefinition.Properties.
                                    Where(pr => pr.Name == DICT_DATA_EXTRACTION_PROP_NAME)
                                    .FirstOrDefault();
        
        ProcessProp(typeDefinition, prop);
        
    }

    void ProcessProp(TypeDefinition typeDefinition, PropertyDefinition prop)
    {
        var name = prop.Name;
        var propType = prop.PropertyType;
        FileInfo fiAssemblyPath = new FileInfo(MainModuleDef.Assembly.MainModule.FullyQualifiedName);
        moduleWeaver.LogInfo(String.Format("Loading {0} assembly file for source object reflection instantiation!", fiAssemblyPath.FullName));

        Type seedSourceReflectionType;
        object instance;

        InstantiateSourceDict(typeDefinition, fiAssemblyPath, out seedSourceReflectionType, out instance);

        IDictionary<string, object> propValues = 
            seedSourceReflectionType
                .GetProperty(DICT_DATA_EXTRACTION_PROP_NAME)
                    .GetValue(instance, null) as IDictionary<string, object>;

        foreach (var dictKeyName in propValues.Keys)
        {
            var ob = propValues[dictKeyName];
            var seedObjecttype = ob.GetType();

            // hard to implement in full correctness
            //var typeRef = MainModuleDef.ImportReference(ob.GetType()).Resolve();
            var typeRef = MainModuleDef.ImportReference(seedObjecttype);

            FieldDefinition fd = new FieldDefinition(dictKeyName, FieldAttributes.Private, typeRef);
            fd.Name = string.Format("<{0}>k__BackingField", dictKeyName);
            fd.IsPublic = false;
            fd.IsPrivate = true;

            typeDefinition.Fields.Add(fd);

            var get = GetGet(fd, name, typeDefinition, dictKeyName, typeRef);
            typeDefinition.Methods.Add(get);

            var set = GetSet(fd, name, typeDefinition, dictKeyName, typeRef);
            typeDefinition.Methods.Add(set);

            var propertyDefinition = new PropertyDefinition(dictKeyName, PropertyAttributes.None, fd.FieldType)
            {
                GetMethod = get,
                SetMethod = set
            };

            foreach (var customAttribute in prop.CustomAttributes)
            {
                propertyDefinition.CustomAttributes.Add(customAttribute);
            }

            prop.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.CompilerGeneratedReference));
            typeDefinition.Properties.Add(propertyDefinition);

            TypeDefinition staticNewSEEDTypeDef = CreatePropertyExtractType(typeDefinition, prop, dictKeyName);

            typeDefinition.Module.Types.Add(staticNewSEEDTypeDef);
        }

    }

    private void InstantiateSourceDict(TypeDefinition typeDefinition, FileInfo fiAssemblyPath, out Type seedSourceReflectionType, out object instance)
    {
        var assemblyPayload = File.ReadAllBytes(fiAssemblyPath.FullName);
        var targetClassicAssembly = System.Reflection.Assembly.Load(assemblyPayload);        
        seedSourceReflectionType = targetClassicAssembly.GetType(typeDefinition.FullName, true, true);
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        instance = null;
        instance = Activator.CreateInstance(seedSourceReflectionType);
    }

    private rf.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {   
        var satelliteAssembly = moduleWeaver.ModuleDefinition.AssemblyResolver.Resolve(args.Name);
        moduleWeaver.LogInfo(String.Format("---| Resolving assembly  [{0}] on behalf of [{1}]",args.Name,args.RequestingAssembly));
        moduleWeaver.LogInfo(String.Format("---| Weavers FQAN [{0}] resolved at [{1}]", moduleWeaver.ModuleDefinition.FullyQualifiedName,satelliteAssembly.FullName));
        moduleWeaver.LogInfo(String.Format("---| satelliteAssembly name [{0}] with argsname[{1}]", satelliteAssembly.Name, args.Name));
        
        foreach (var reff in moduleWeaver.ReferenceCopyLocalPaths)
        {
            moduleWeaver.LogInfo(String.Format("---|-- referenced module:[{0}] ", new FileInfo(reff).Name));
        }

        var referenceFiles = moduleWeaver.ReferenceCopyLocalPaths
            .Select(p => new FileInfo(p))
            .ToList<FileInfo>();

        var assemblyFile = referenceFiles
                                .Where(rf => rf.Name.Split('.').Count()>0 &&
                                                rf.Name.Split('.')[0]==( (args.Name.Split(',')[0])))
                                .FirstOrDefault();
        moduleWeaver.LogInfo(String.Format("---|-- RESOLVED: assembly file to load:[{0}] ", assemblyFile?.FullName));

        if (assemblyFile != null)
        {
            return rf.Assembly.ReflectionOnlyLoadFrom(assemblyFile.FullName);
        }
        else
        {
            moduleWeaver.LogErrorPoint(String.Format("---| Assembly file not found {0} at {1}", referenceFiles[0], args.Name.Split(',')[0]), new SequencePoint(new Document("")));
        }
        return null;
    }

    private static TypeDefinition CreatePropertyExtractType(TypeDefinition typeDefinition, PropertyDefinition prop, string dictKeyName)
    {
        var strFullNewTypeNamespace = String.Format("{0}.{1}Source", typeDefinition.Namespace, typeDefinition.Name);
        TypeDefinition staticNewSEEDTypeDef = new TypeDefinition(strFullNewTypeNamespace, dictKeyName, TypeAttributes.Class);
        staticNewSEEDTypeDef.IsPublic = true;
        staticNewSEEDTypeDef.BaseType = typeDefinition.BaseType;
        return staticNewSEEDTypeDef;
    }

    MethodDefinition GetGet(FieldDefinition field, string name, TypeDefinition tdef,string dictKey, TypeReference seedObjectTypeReference)
    {
        var get = new MethodDefinition("get_" + name,
                                       MethodAttributes.Public |                                    
                                       MethodAttributes.SpecialName |                              
                                       MethodAttributes.HideBySig, field.FieldType);
        var instructions = get.Body.Instructions;
        instructions.Add(Instruction.Create(OpCodes.Nop));
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        var getSeedKeyValueMethReference = tdef.Methods
                               .Where(m => m.Name == "GetKeyValue")
                               .FirstOrDefault();
        var callInstrDict = Instruction.Create(OpCodes.Call,getSeedKeyValueMethReference);

        instructions.Add(Instruction.Create(OpCodes.Ldstr, dictKey));
        instructions.Add(callInstrDict);
        
        
        instructions.Add(Instruction.Create(OpCodes.Castclass, seedObjectTypeReference));
        instructions.Add(Instruction.Create(OpCodes.Stloc_0));
        var inst = Instruction.Create(OpCodes.Ldloc_0);
        instructions.Add(Instruction.Create(OpCodes.Br_S, inst));
        instructions.Add(inst);
        instructions.Add(Instruction.Create(OpCodes.Ret));        

        get.Body.Variables.Add(new VariableDefinition(field.FieldType));
        get.Body.InitLocals = true;
        get.SemanticsAttributes = MethodSemanticsAttributes.Getter;
        get.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.CompilerGeneratedReference));
        return get;
    }

    MethodDefinition GetSet(FieldDefinition field, string name, TypeDefinition tdef, string dictKey, TypeReference seedObjectTypeReference)
    {
        var set = new MethodDefinition("set_" + name,
                                       MethodAttributes.Public | MethodAttributes.SpecialName |                          
                                       MethodAttributes.HideBySig, typeSystem.Void);
        var instructions = set.Body.Instructions;
        instructions.Add(Instruction.Create(OpCodes.Nop));
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        instructions.Add(Instruction.Create(OpCodes.Ldstr,dictKey));
        instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
        var setSeedKeyValueMethReference = tdef.Methods
                               .Where(m => m.Name == "SetKeyValue")
                               .FirstOrDefault();
        instructions.Add(Instruction.Create(OpCodes.Call, setSeedKeyValueMethReference));
        instructions.Add(Instruction.Create(OpCodes.Nop));
        instructions.Add(Instruction.Create(OpCodes.Ret));

        set.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, field.FieldType));
        set.SemanticsAttributes = MethodSemanticsAttributes.Setter;
        set.CustomAttributes.Add(new CustomAttribute(msCoreReferenceFinder.CompilerGeneratedReference));
        return set;
    }

    public void Execute()
    {
        foreach (var type in allTypes)
        {
            if (type.IsInterface)
            {
                continue;
            }
            if (type.IsValueType)
            {
                continue;
            }
            if (type.IsEnum)
            {
                continue;
            }
            Process(type);
        }
    }
}