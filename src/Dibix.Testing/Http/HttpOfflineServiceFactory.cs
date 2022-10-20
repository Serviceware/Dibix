using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    public static class HttpOfflineServiceFactory
    {
        #region Fields
        private static readonly Lazy<ModuleBuilder> ModuleBuilderAccessor = new Lazy<ModuleBuilder>(CreateModuleBuilder);
        private static readonly Type[] ConstructorSignature =
        {
            typeof(IHttpClientFactory),
            typeof(IHttpAuthorizationProvider)
        };
        #endregion

        #region Public Methods
        public static TService For<TService, TBuilder>(Expression<Action<TBuilder>> setup) where TBuilder : IHttpTestServiceBuilder<TService>
        {
            if (!(setup.Body is MethodCallExpression callExpression))
                throw new ArgumentException("Not a valid method expression", nameof(setup));

            Type contractType = typeof(TService);
            Type implementationType = ResolveImplementationType(contractType);

            string targetMethodName = callExpression.Method.Name;
            MethodInfo implementationMethod = implementationType.SafeGetMethod(targetMethodName);
            MethodInfo contractMethod = contractType.SafeGetMethod(targetMethodName);

            TypeBuilder typeBuilder = ModuleBuilderAccessor.Value.DefineType($"{implementationType.FullName}Proxy", TypeAttributes.NotPublic | TypeAttributes.Sealed);
            typeBuilder.AddInterfaceImplementation(contractType);

            MethodBuilder delegatedMethodBuilder = BuildDelegatedMethod(typeBuilder, implementationMethod);

            foreach (MethodInfo methodToImplement in contractType.GetMethods())
            {
                if (methodToImplement == contractMethod)
                    BuildDelegatedMethodImplementation(typeBuilder, methodToImplement, delegatedMethodBuilder);
                else
                    BuildEmptyMethodImplementation(typeBuilder, methodToImplement);
            }

            Type type = typeBuilder.CreateType();
            TService service = (TService)Activator.CreateInstance(type);
            return service;
        }
        #endregion

        #region Private Methods
        private static MethodBuilder BuildDelegatedMethod(TypeBuilder typeBuilder, MethodInfo targetMethod)
        {
            ICollection<ParameterExpression> parameters = new Collection<ParameterExpression>();
            foreach (ParameterInfo parameter in targetMethod.GetParameters())
                parameters.Add(Expression.Parameter(parameter.ParameterType, parameter.Name));

            ConstructorInfo httpClientFactoryConstructor = typeof(DefaultHttpClientFactory).GetConstructor(new[] { typeof(HttpClientConfiguration[]) });
            if (httpClientFactoryConstructor == null)
                throw new InvalidOperationException($"Could not find constructor {typeof(DefaultHttpClientFactory)}({typeof(HttpClientConfiguration)}[])");

            Expression offlineClientConfiguration = Expression.New(typeof(OfflineHttpClientConfiguration));
            Expression httpClientFactory = Expression.New(httpClientFactoryConstructor, Expression.NewArrayInit(typeof(HttpClientConfiguration), offlineClientConfiguration));
            Expression httpAuthorizationProvider = Expression.New(typeof(EmptyHttpAuthorizationProvider));
            ConstructorInfo constructor = targetMethod.DeclaringType.GetConstructor(ConstructorSignature);
            Expression instance = Expression.New(constructor, httpClientFactory, httpAuthorizationProvider);
            Expression call = Expression.Call(instance, targetMethod, parameters);
            LambdaExpression lambda = Expression.Lambda(call, parameters);
            MethodBuilder methodDelegateBuilder = typeBuilder.DefineMethod($"{targetMethod.Name}Impl", MethodAttributes.Private | MethodAttributes.Static);
#if !NETFRAMEWORK
            throw new PlatformNotSupportedException("LambdaExpression.CompileToMethod is not supported on this platform");
#else
            lambda.CompileToMethod(methodDelegateBuilder);
            return methodDelegateBuilder;
#endif
        }

        private static void BuildDelegatedMethodImplementation(TypeBuilder typeBuilder, MethodInfo methodToImplement, MethodBuilder delegatedMethodBuilder)
        {
            BuildMethodImplementation(typeBuilder, methodToImplement, (parameterTypes, ilGenerator) =>
            {
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                }

                MethodInfo method = delegatedMethodBuilder.GetBaseDefinition();
                ilGenerator.EmitCall(OpCodes.Call, method, parameterTypes);
                ilGenerator.Emit(OpCodes.Ret);
            });
        }

        private static void BuildEmptyMethodImplementation(TypeBuilder typeBuilder, MethodInfo methodToImplement)
        {
            BuildMethodImplementation(typeBuilder, methodToImplement, (parameterTypes, ilGenerator) =>
            {
                Type exceptionType = typeof(NotImplementedException);
                ConstructorInfo exceptionCtor = exceptionType.GetConstructor(Type.EmptyTypes);
                if (exceptionCtor == null)
                    throw new InvalidOperationException($"Could not find a parameterless constructor on type: '{exceptionType}'");

                ilGenerator.Emit(OpCodes.Newobj, exceptionCtor);
                ilGenerator.Emit(OpCodes.Throw);
            });
        }

        private static void BuildMethodImplementation(TypeBuilder typeBuilder, MethodInfo methodToImplement, Action<Type[], ILGenerator> body)
        {
            Type[] parameterTypes = methodToImplement.GetParameters()
                                                     .Select(x => x.ParameterType)
                                                     .ToArray();

            MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodToImplement.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodToImplement.ReturnType, parameterTypes);
            typeBuilder.DefineMethodOverride(methodBuilder, methodToImplement);

            if (body == null)
                return;

            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            body(parameterTypes, ilGenerator);
        }

        private static Type ResolveImplementationType(Type contractType)
        {
            foreach (Type type in contractType.Assembly.GetTypes())
            {
                HttpServiceAttribute attribute = type.GetCustomAttribute<HttpServiceAttribute>();
                if (attribute?.ContractType == contractType)
                    return type;
            }

            throw new InvalidOperationException($"Could not determine server implementation for type '{contractType}'. Is it a HTTP service generated by Dibix?");
        }

        private static ModuleBuilder CreateModuleBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(typeof(HttpOfflineServiceFactory).Assembly.FullName);
            assemblyName.Name += ".Proxy";
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            return moduleBuilder;
        }
        #endregion
    }
}