using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

public static class CecilExtensions
{

    public static bool ContainsAttribute(this IEnumerable<CustomAttribute> attributes, string attributeName)
    {
        return attributes.Any(attribute => attribute.Constructor.DeclaringType.Name == attributeName);
    }
    public static bool IsRefOrOut(this Instruction next)
    {
        if (next.OpCode == OpCodes.Call || next.OpCode == OpCodes.Calli)
        {
            var methodReference = next.Operand as MethodReference;
            if (methodReference != null)
            {
                return methodReference.Parameters.Any(x => x.IsOut || x.ParameterType.Name.EndsWith("&"));
            }
        }
        return false;
    }
    public static SequencePoint FindSequencePoint(this Instruction instruction)
    {
        while (instruction != null)
        {
            if (instruction.SequencePoint != null)
            {
                return instruction.SequencePoint;
            }
            instruction = instruction.Previous;
        }
        return null;
    }

    public static MethodReference MakeGenericMethod(this MethodReference self, params TypeReference[] arguments)
    {
        if (self.GenericParameters.Count != arguments.Length)
            throw new ArgumentException();

        var instance = new GenericInstanceMethod(self);
        foreach (var argument in arguments)
            instance.GenericArguments.Add(argument);

        return instance;
    }

    public static MethodReference MakeGeneric(this MethodReference self, params TypeReference[] arguments)
    {
        var reference = new MethodReference(self.Name,self.ReturnType)
        {
            Name = self.Name,
            DeclaringType = self.DeclaringType.MakeGenericType(arguments),
            HasThis = self.HasThis,
            ExplicitThis = self.ExplicitThis,
            ReturnType = self.ReturnType,
            CallingConvention = self.CallingConvention,
        };

        foreach (var parameter in self.Parameters)
            reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

        foreach (var generic_parameter in self.GenericParameters)
            reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

        return reference;
    }

    public static TypeReference MakeGenericType(this TypeReference self, params TypeReference[] arguments)
    {
        if (self.GenericParameters.Count != arguments.Length)
            throw new ArgumentException();

        var instance = new GenericInstanceType(self);
        foreach (var argument in arguments)
            instance.GenericArguments.Add(argument);

        return instance;
    }
}