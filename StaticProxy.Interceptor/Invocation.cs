﻿// portions of this code have been taken from the Castle project (http://www.castleproject.org/), a big thanks to the authors.

namespace StaticProxy.Interceptor
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using StaticProxy.Interceptor.TargetInvocation;

    internal class Invocation : IInvocation
    {
        private readonly ITargetInvocation targetInvocation;
        private readonly MethodInfo decoratedMethod;
        private readonly object[] arguments;
        private readonly IDynamicInterceptor[] interceptors;
        private readonly Lazy<Type[]> parameterTypes;
        private readonly Lazy<Type[]> genericArguments;

        private int currentInterceptorIndex = -1;

        public Invocation(ITargetInvocation targetInvocation, MethodInfo decoratedMethod, object[] arguments, IDynamicInterceptor[] interceptors)
        {
            this.targetInvocation = targetInvocation;
            this.decoratedMethod = decoratedMethod;
            this.arguments = arguments;
            this.interceptors = interceptors;

            this.parameterTypes = new Lazy<Type[]>(() => this.decoratedMethod.GetParameters().Select(x => x.ParameterType).ToArray());

            this.genericArguments = new Lazy<Type[]>(() => this.decoratedMethod.GetGenericArguments());
        }

        public object[] Arguments
        {
            get { return this.arguments; }
        }

        public Type[] GenericArguments
        {
            get { return this.genericArguments.Value; }
        }

        public MethodInfo Method
        {
            get { return this.decoratedMethod; }
        }

        public object ReturnValue { get; set; }

        public object GetArgumentValue(int index)
        {
            return this.arguments[index];
        }

        public void Proceed()
        {
            this.currentInterceptorIndex++;
            try
            {
                if (this.currentInterceptorIndex == this.interceptors.Length)
                {
                    this.ReturnValue = this.targetInvocation.InvokeMethodOnTarget(this.arguments);
                }
                else if (this.currentInterceptorIndex > this.interceptors.Length)
                {
                    throw this.CreateExceptionForInvalidCurrentInterceptorIndex();
                }
                else
                {
                    this.interceptors[this.currentInterceptorIndex].Intercept(this);
                }
            }
            finally
            {
                this.currentInterceptorIndex--;
            }
        }

        public void SetArgumentValue(int index, object value)
        {
            var expectedParameterType = this.parameterTypes.Value[index];
            if (value == null)
            {
                if (expectedParameterType.GetTypeInfo().IsValueType)
                {
                    throw new ArgumentNullException(
                        index.ToString(),
                        string.Format(CultureInfo.InvariantCulture, "Cannot set value-type ({0}) parameter to null", expectedParameterType));
                }
            }
            else
            {
                var actualParameterType = value.GetType();
                if (!expectedParameterType.GetTypeInfo().IsAssignableFrom(actualParameterType.GetTypeInfo()))
                {
                    throw new ArgumentOutOfRangeException(
                        index.ToString(),
                        string.Format(CultureInfo.InvariantCulture, "Cannot set {0} parameter to value of type {1}", expectedParameterType, actualParameterType));
                }
            }
            
            this.arguments[index] = value;
        }

        private InvalidOperationException CreateExceptionForInvalidCurrentInterceptorIndex()
        {
            string interceptorsCount;
            if (this.interceptors.Length > 1)
            {
                interceptorsCount = " each one of " + this.interceptors.Length + " interceptors";
            }
            else
            {
                interceptorsCount = " interceptor";
            }

            var message = "This is a StaticProxy.Fody error: invocation.Proceed() has been called more times than expected."
                          + "This usually signifies a bug in the calling code. Make sure that" + interceptorsCount
                          + " selected for the method '" + this.Method + "'" + "calls invocation.Proceed() at most once.";
            return new InvalidOperationException(message);
        }
    }
}