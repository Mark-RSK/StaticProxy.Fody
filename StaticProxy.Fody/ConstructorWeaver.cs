﻿namespace StaticProxy.Fody
{
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    using StaticProxy.Interceptor;

    public class ConstructorWeaver
    {
        private readonly TypeReference interceptorManagerReference;
        private readonly MethodReference managerInitializeMethodReference;

        private readonly MethodReference ensureDynamicInterceptorManagerNotNull;

        public ConstructorWeaver()
        {
            this.interceptorManagerReference = WeavingInformation.DynamicInterceptorManagerReference;
            this.managerInitializeMethodReference = WeavingInformation.ModuleDefinition.Import(this.interceptorManagerReference.Resolve().GetMethods().Single(x => x.Name == "Initialize"));

            TypeDefinition exceptions = WeavingInformation.ModuleDefinition.Import(typeof(Exceptions)).Resolve();
            this.ensureDynamicInterceptorManagerNotNull = WeavingInformation.ModuleDefinition.Import(exceptions.GetMethods().Single(x => x.Name == "EnsureDynamicInterceptorManagerNotNull"));
        }

        public FieldDefinition ExtendConstructorWithDynamicInterceptorRetriever(TypeDefinition typeToProxy)
        {
            FieldDefinition field = AddPrivateReadonlyField(typeToProxy, this.interceptorManagerReference);

            MethodDefinition constructor = typeToProxy.GetConstructors().Single();
            constructor.Body.InitLocals = true;

            ParameterDefinition parameter = AddParameter(constructor, this.interceptorManagerReference);

            Instruction firstInstruction = FindFirstInstruction(constructor);

            ILProcessor processor = constructor.Body.GetILProcessor();

            processor.InsertBefore(firstInstruction, InstructionHelper.CallMethodAndPassParameter(this.ensureDynamicInterceptorManagerNotNull, parameter));
            processor.InsertBefore(firstInstruction, InstructionHelper.SetInstanceFieldToMethodParameter(field, parameter));
            processor.InsertBefore(firstInstruction, InstructionHelper.CallMethodAndPassThis(field, this.managerInitializeMethodReference));

            return field;
        }

        private static Instruction FindFirstInstruction(MethodDefinition constructor)
        {
            Instruction instruction = constructor.Body.Instructions.First(i => i.OpCode == OpCodes.Call).Next;
            while (instruction.OpCode == OpCodes.Nop)
            {
                instruction = instruction.Next;
            }

            return instruction;
        }

        private static FieldDefinition AddPrivateReadonlyField(TypeDefinition typeToProxy, TypeReference typeOfField)
        {
            var field = new FieldDefinition(
                typeOfField.Name,
                FieldAttributes.Private | FieldAttributes.InitOnly,
                typeOfField);
            typeToProxy.Fields.Add(field);
            return field;
        }

        private static ParameterDefinition AddParameter(MethodDefinition method, TypeReference typeOfParameter)
        {
            var parameter = new ParameterDefinition(
                typeOfParameter.Name, 
                ParameterAttributes.None,
                typeOfParameter);
            method.Parameters.Add(parameter);
            return parameter;
        }
    }
}