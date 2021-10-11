using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Dibix.Http.Server
{
    internal sealed class ReflectionHttpActionTargetProxyBuilder
    {
        private readonly ModuleBuilder _moduleBuilder;
        private readonly IDictionary<Type, TypeBuilder> _typeBuilderMap;
        private readonly IDictionary<Type, Type> _proxyTypeMap;

        private ReflectionHttpActionTargetProxyBuilder(ModuleBuilder moduleBuilder)
        {
            this._moduleBuilder = moduleBuilder;
            this._typeBuilderMap = new Dictionary<Type, TypeBuilder>();
            this._proxyTypeMap = new Dictionary<Type, Type>();
        }

        public static ReflectionHttpActionTargetProxyBuilder Create()
        {
            AssemblyName assemblyName = new AssemblyName(typeof(ReflectionHttpActionTargetProxyBuilder).Assembly.FullName);
            assemblyName.Name += ".Proxy";
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            return new ReflectionHttpActionTargetProxyBuilder(moduleBuilder);
        }

        public void AddMethod(MethodInfo method)
        {
            Type type = method.DeclaringType;
            if (!this._typeBuilderMap.TryGetValue(type, out TypeBuilder typeBuilder))
            {
                typeBuilder = this.AddType(type);
                this._typeBuilderMap.Add(type, typeBuilder);
            }
            AddMethod(typeBuilder, method);
        }

        public MethodInfo GetProxyMethod(MethodInfo method)
        {
            Type declaringType = method.DeclaringType;
            if (!this._proxyTypeMap.TryGetValue(declaringType, out Type proxyType))
            {
                if (!this._typeBuilderMap.TryGetValue(declaringType, out TypeBuilder typeBuilder))
                    throw new InvalidOperationException($"No proxy for type '{declaringType}' is registered. Did you call '{nameof(AddMethod)}' on this instance?");

                proxyType = typeBuilder.CreateTypeInfo();
                this._proxyTypeMap.Add(declaringType, proxyType);
            }

            return proxyType.GetMethod(method.Name);
        }

        private static void AddMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            ICollection<ParameterExpression> parameters = new Collection<ParameterExpression>();
            ICollection<ParameterExpression> variables = new Collection<ParameterExpression>();
            ICollection<Expression> methodParameters = new Collection<Expression>();

            foreach (ParameterInfo parameter in method.GetParameters())
            {
                ParameterExpression parameterExpression;
                if (!parameter.ParameterType.IsByRef)
                {
                    parameterExpression = Expression.Parameter(parameter.ParameterType, parameter.Name);
                    parameters.Add(parameterExpression);
                }
                else
                {
                    parameterExpression = Expression.Variable(parameter.ParameterType.GetElementType(), parameter.Name);
                    variables.Add(parameterExpression);
                }
                methodParameters.Add(parameterExpression);
            }

            MethodCallExpression call = Expression.Call(method, methodParameters);
            BlockExpression block = Expression.Block(variables, call);
            LambdaExpression lambda = Expression.Lambda(block, parameters);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public | MethodAttributes.Static);
#if NET5_0 || NETSTANDARD
            throw new PlatformNotSupportedException("LambdaExpression.CompileToMethod is not supported on this platform");
#else
            lambda.CompileToMethod(methodBuilder);
#endif
        }

        private TypeBuilder AddType(Type type) => this._moduleBuilder.DefineType(type.FullName, TypeAttributes.Public);
    }
}