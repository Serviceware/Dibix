using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace Dibix.Http
{
    public static class HttpParameterResolver
    {
        #region Fields
        private static readonly ICollection<Type> KnownDependencies = new[] { typeof(IDatabaseAccessorFactory) };
        private static readonly Lazy<PropertyAccessor> DebugViewAccessor = new Lazy<PropertyAccessor>(BuildDebugViewAccessor);
        private const string ItemSourceName = "ITEM";
        private const string ItemIndexPropertyName = "$INDEX";
        #endregion

        #region Delegates
        private delegate void ResolveParameters(HttpRequestMessage request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver);
        #endregion

        #region Public Methods
        public static IHttpParameterResolutionMethod Compile(HttpActionDefinition action, ICollection<ParameterInfo> parameters)
        {
            HttpParameterResolverCompilationContext context = new HttpParameterResolverCompilationContext();
            try
            {
                ICollection<HttpParameterInfo> @params = CollectParameters(action, parameters, context).ToArray();

                // (HttpRequestMessage request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver) => 
                ParameterExpression requestParameter = Expression.Parameter(typeof(HttpRequestMessage), "request");
                ParameterExpression argumentsParameter = Expression.Parameter(typeof(IDictionary<string, object>), "arguments");
                ParameterExpression dependencyResolverParameter = Expression.Parameter(typeof(IParameterDependencyResolver), "dependencyResolver");
                context.Parameters.Add(requestParameter);
                context.Parameters.Add(argumentsParameter);
                context.Parameters.Add(dependencyResolverParameter);

                // IDatabaseAccesorFactory databaseAccesorFactory = dependencyResolver.Resolve<IDatabaseAccesorFactory>();
                // IActionAuthorizationContext actionAuthorizationContext = dependencyResolver.Resolve<IActionAuthorizationContext>();
                // ...
                IDictionary<string, HttpParameterSourceExpression> sources = CollectParameterSources(action, @params, requestParameter, argumentsParameter, dependencyResolverParameter, context);

                // SomeValueFromBody body = HttpParameterResolver.ReadBody<SomeValueFromBody>(arguments);
                //
                // arguments.Add("databaseAccessorFactory", databaseAccessorFactory);
                // arguments.Add("lcid", actionAuthorizationContext.LocaleId);
                // arguments.Add("id", body.Id);
                //
                // SomeInputParameterClass input = new SomeInputParameterClass();
                // input.lcid = actionAuthorizationContext.LocaleId;
                // input.id = HttpParameterResolver.ConvertValue<object, int>("id", arguments["id"]);
                // HttpParameterResolver.BindParametersFromBody<SomeValueFromBody, SomeInputParameterClass, SomeInputBinder>(arguments, input, "input");
                // arguments.Add("input", input);
                //
                // input.someUdt1 = HttpParameterResolver.ConvertParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments);
                // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments, "someUdt1");
                // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt2, SomeUdt2InputConverter>(arguments, "someUdt2");
                // ...
                CollectParameterAssignments(action, @params, argumentsParameter, sources, context);

                ResolveParameters compiled = Compile(context, out string source);
                HttpParameterResolutionMethod method = new HttpParameterResolutionMethod(source, compiled);
                CollectExpectedParameters(method, @params, action.BodyContract);
                return method;
            }
            catch (Exception exception)
            {
                throw CreateException("Http parameter resolver compilation failed", exception, action, context.LastVisitedParameter);
            }
        }
        #endregion

        #region Private Methods
        private static IEnumerable<HttpParameterInfo> CollectParameters(HttpActionDefinition action, IEnumerable<ParameterInfo> parameters, HttpParameterResolverCompilationContext context)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                context.LastVisitedParameter = parameter.Name;

                if (KnownDependencies.Contains(parameter.ParameterType))
                {
                    string sourceName = parameter.ParameterType.Name.TrimStart('I');
                    sourceName = String.Concat(Char.ToLowerInvariant(sourceName[0]), sourceName.Substring(1));
                    yield return HttpParameterInfo.SourceInstance(parameter.ParameterType, parameter.Name, new HttpParameterDependencySourceProvider(parameter.ParameterType), sourceName);
                    continue;
                }

                if (parameter.IsDefined(typeof(InputClassAttribute)))
                {
                    foreach (PropertyInfo property in parameter.ParameterType.GetRuntimeProperties())
                    {
                        if (action.ParameterSources.TryGetValue(property.Name, out HttpParameterSource parameterSource))
                        {
                            yield return CollectExplicitParameter(parameterSource, parameter, property.PropertyType, property.Name);
                        }
                        else if (action.BodyBinder != null)
                        {
                            yield return HttpParameterInfo.Body(parameter, property.PropertyType, property.Name);
                        }
                        else
                        {
                            yield return CollectImplicitParameter(action, parameter, property.PropertyType, property.Name, false);
                        }
                    }
                }
                else
                {
                    if (action.ParameterSources.TryGetValue(parameter.Name, out HttpParameterSource parameterSource))
                    {
                        yield return CollectExplicitParameter(parameterSource, null, parameter.ParameterType, parameter.Name);
                    }
                    else if (action.BodyBinder != null)
                    {
                        throw new InvalidOperationException($"Using a binder for the body is only supported if the target parameter is a class and is marked with the {typeof(InputClassAttribute)}");
                    }
                    else
                    {
                        yield return CollectImplicitParameter(action, null, parameter.ParameterType, parameter.Name, parameter.IsOptional);
                    }
                }
            }

            context.LastVisitedParameter = null;
        }

        private static HttpParameterInfo CollectExplicitParameter(HttpParameterSource source, ParameterInfo contractParameter, Type parameterType, string parameterName)
        {
            switch (source)
            {
                case HttpParameterPropertySource propertySource:
                    IHttpParameterSourceProvider sourceProvider = GetSourceProvider(propertySource.SourceName);
                    bool isItemsParameter = typeof(StructuredType).GetTypeInfo().IsAssignableFrom(parameterType);
                    if (!isItemsParameter)
                    {
                        IHttpParameterConverter converter = null;
                        if (!String.IsNullOrEmpty(propertySource.ConverterName))
                            converter = GetConverter(propertySource.ConverterName);

                        return HttpParameterInfo.SourceProperty(contractParameter, parameterType, parameterName, propertySource.SourceName, sourceProvider, propertySource.PropertyName, propertySource.ConverterName, converter);
                    }

                    return CollectItemsParameter(contractParameter, parameterType, parameterName, propertySource, sourceProvider);

                case HttpParameterBodySource bodySource:
                    Type inputConverter = Type.GetType(bodySource.ConverterName, true);
                    return HttpParameterInfo.Body(contractParameter, parameterType, parameterName, inputConverter);

                case HttpParameterConstantSource constantSource:
                    return HttpParameterInfo.ConstantValue(contractParameter, parameterType, parameterName, constantSource.Value);

                default:
                    throw new InvalidOperationException($"Unsupported parameter source type: '{source.GetType()}'");
            }
        }

        private static HttpParameterInfo CollectImplicitParameter(HttpActionDefinition action, ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional)
        {
            if (action.BodyContract == null)
                return HttpParameterInfo.Uri(contractParameter, parameterType, parameterName, isOptional);

            string sourcePropertyName = parameterName;
            PropertyInfo sourceProperty = action.BodyContract.GetTypeInfo().GetProperty(sourcePropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (sourceProperty == null)
                return HttpParameterInfo.Uri(contractParameter, parameterType, parameterName, isOptional);

            sourcePropertyName = sourceProperty.Name;
            IHttpParameterSourceProvider sourceProvider = GetSourceProvider(BodyParameterSourceProvider.SourceName);

            bool isItemsParameter = typeof(StructuredType).GetTypeInfo().IsAssignableFrom(parameterType);
            if (isItemsParameter)
                return CollectItemsParameter(contractParameter, parameterType, parameterName, new HttpParameterPropertySource(BodyParameterSourceProvider.SourceName, sourcePropertyName, null), sourceProvider);

            return HttpParameterInfo.SourceProperty(contractParameter, parameterType, parameterName, BodyParameterSourceProvider.SourceName, sourceProvider, sourcePropertyName, null, null);
        }

        private static IHttpParameterSourceProvider GetSourceProvider(string sourceName)
        {
            if (sourceName == ItemSourceName)
                return null;

            if (!HttpParameterSourceProviderRegistry.TryGetProvider(sourceName, out IHttpParameterSourceProvider sourceProvider))
                throw new InvalidOperationException($"No source with the name '{sourceName}' is registered");

            return sourceProvider;
        }

        private static IHttpParameterConverter GetConverter(string converterName)
        {
            if (!HttpParameterConverterRegistry.TryGetConverter(converterName, out IHttpParameterConverter converter))
                throw new InvalidOperationException($"No converter with the name '{converterName}' is registered");

            return converter;
        }

        private static HttpParameterInfo CollectItemsParameter(ParameterInfo contractParameter, Type parameterType, string parameterName, HttpParameterPropertySource propertySource, IHttpParameterSourceProvider sourceProvider)
        {
            string udtName = parameterType.GetTypeInfo().GetCustomAttribute<StructuredTypeAttribute>()?.UdtName;
            MethodInfo addMethod = parameterType.GetTypeInfo().GetMethod("Add");
            if (addMethod == null)
                throw new InvalidOperationException($"Could not find 'Add' method on type: {parameterType}");

            IDictionary<string, Type> parameterMap = addMethod.GetParameters().ToDictionary(x => x.Name, x => x.ParameterType);
            IEnumerable<HttpParameterInfo> itemSources = propertySource.ItemSources.Select(x =>
            {
                if (!parameterMap.TryGetValue(x.Key, out Type itemParameterType))
                    throw new InvalidOperationException($"Target name does not match a UDT column: {udtName ?? parameterType.ToString()}.{x.Key}");

                return CollectExplicitParameter(x.Value, null, itemParameterType, x.Key);
            });
            return HttpParameterInfo.SourcePropertyItemsSource(contractParameter, parameterType, parameterName, sourceProvider, propertySource.SourceName, propertySource.PropertyName, udtName, addMethod, itemSources);
        }

        private static IDictionary<string, HttpParameterSourceExpression> CollectParameterSources(HttpActionDefinition action, IEnumerable<HttpParameterInfo> @params, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyResolverParameter, HttpParameterResolverCompilationContext context)
        {
            IDictionary<string, HttpParameterSourceExpression> sources = @params.Where(x => x.SourceKind == HttpParameterSourceKind.SourceInstance
                                                                                         || x.SourceKind == HttpParameterSourceKind.SourceProperty)
                                                                                .GroupBy(x => new { SourceProvider = x.Source.Instance, x.Source.Name })
                                                                                .Select(x => BuildParameterSource(action, x.Key.Name, x.Key.SourceProvider, requestParameter, argumentsParameter, dependencyResolverParameter))
                                                                                .ToDictionary(x => x.Key, x => x.Value);
            foreach (HttpParameterSourceExpression source in sources.Values)
            {
                context.Variables.Add(source.Variable);
                context.Statements.Add(source.Assignment);
            }

            return sources;
        }

        private static KeyValuePair<string, HttpParameterSourceExpression> BuildParameterSource(HttpActionDefinition action, string sourceName, IHttpParameterSourceProvider sourceProvider, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyResolverParameter)
        {
            Type instanceType = sourceProvider.GetInstanceType(action);
            ParameterExpression sourceVariable = Expression.Variable(instanceType, $"{sourceName.ToLowerInvariant()}Source");
            Expression sourceAssign = Expression.Assign(sourceVariable, sourceProvider.GetInstanceValue(instanceType, requestParameter, argumentsParameter, dependencyResolverParameter));
            return new KeyValuePair<string, HttpParameterSourceExpression>(sourceName, new HttpParameterSourceExpression(sourceVariable, sourceAssign));
        }

        private static void CollectParameterAssignments(HttpActionDefinition action, IEnumerable<HttpParameterInfo> parameters, ParameterExpression argumentsParameter, IDictionary<string, HttpParameterSourceExpression> sources, HttpParameterResolverCompilationContext context)
        {
            foreach (IGrouping<ParameterInfo, HttpParameterInfo> parameterGroup in parameters.GroupBy(x => x.ContractParameter))
            {
                ParameterExpression contractParameterVariable = null;
                if (parameterGroup.Key != null)
                {
                    context.LastVisitedParameter = parameterGroup.Key.Name;

                    // SomeInputParameterClass input = new SomeInputParameterClass();
                    contractParameterVariable = CollectInputClass(context, parameterGroup.Key);
                }

                foreach (HttpParameterInfo parameter in parameterGroup)
                {
                    context.LastVisitedParameter = parameter.ParameterName;

                    if (contractParameterVariable == null)
                    {
                        if (parameter.SourceKind == HttpParameterSourceKind.Body)
                        {
                            // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments, "someUdt1");
                            CollectBodyParameterAssignment(context, action.SafeGetBodyContract(), parameter, argumentsParameter);
                        }
                        else if (!parameter.IsUserParameter)
                        {
                            // arguments.Add("lcid", actionAuthorizationContext.LocaleId);
                            CollectNonUserParameterAssignment(context, parameter, argumentsParameter, sources);
                        }
                    }
                    else
                    {
                        if (parameter.SourceKind == HttpParameterSourceKind.Body)
                        {
                            if (parameter.InputConverter != null)
                            {
                                // input.someUdt1 = HttpParameterResolver.ConvertParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments);
                                CollectBodyPropertyAssignment(context, action.SafeGetBodyContract(), parameter, contractParameterVariable, argumentsParameter);
                            }
                        }
                        else if (!parameter.IsUserParameter)
                        {
                            // input.lcid = actionAuthorizationContext.LocaleId;
                            CollectNonUserPropertyAssignment(context, parameter, contractParameterVariable, sources);
                        }
                        else
                        {
                            // input.id = HttpParameterResolver.ConvertValue<object, int>("id", arguments["id"]);
                            CollectUserPropertyAssignment(context, parameter, contractParameterVariable, argumentsParameter);
                        }
                    }
                }

                if (contractParameterVariable != null)
                {
                    context.LastVisitedParameter = parameterGroup.Key.Name;

                    if (action.BodyBinder != null)
                    {
                        // HttpParameterResolver.BindParametersFromBody<SomeValueFromBody, SomeInputParameterClass, SomeInputBinder>(arguments, input);
                        CollectBodyParameterBindingAssignment(context, action.SafeGetBodyContract(), action.BodyBinder, parameterGroup.Key, contractParameterVariable, argumentsParameter);
                    }

                    // arguments.Add("input", input);
                    CollectParameterAssignment(context, contractParameterVariable.Name, argumentsParameter, contractParameterVariable);
                }
            }

            context.LastVisitedParameter = null;
        }

        private static ParameterExpression CollectInputClass(HttpParameterResolverCompilationContext context, ParameterInfo parameter)
        {
            ParameterExpression complexParameterVariable = Expression.Variable(parameter.ParameterType, parameter.Name);
            Expression complexParameterInstance = Expression.New(parameter.ParameterType);
            Expression complexParameterAssign = Expression.Assign(complexParameterVariable, complexParameterInstance);
            context.Variables.Add(complexParameterVariable);
            context.Statements.Add(complexParameterAssign);
            return complexParameterVariable;
        }

        private static void CollectNonUserParameterAssignment(HttpParameterResolverCompilationContext context, HttpParameterInfo parameter, Expression target, IDictionary<string, HttpParameterSourceExpression> sources)
        {
            // arguments.Add("lcid", actionAuthorizationContext.LocaleId);
            Expression value = CollectParameterValue(parameter, sources);
            CollectParameterAssignment(context, parameter.ParameterName, target, value);
        }

        private static void CollectNonUserPropertyAssignment(HttpParameterResolverCompilationContext context, HttpParameterInfo parameter, Expression target, IDictionary<string, HttpParameterSourceExpression> sources)
        {
            // input.lcid = actionAuthorizationContext.LocaleId;
            Expression property = Expression.Property(target, parameter.ParameterName);
            Expression value = CollectParameterValue(parameter, sources);
            Expression assign = Expression.Assign(property, value);
            context.Statements.Add(assign);
        }

        private static void CollectUserPropertyAssignment(HttpParameterResolverCompilationContext context, HttpParameterInfo parameter, Expression target, Expression argumentsParameter)
        {
            // input.id = HttpParameterResolver.ConvertValue<object, int>("id", arguments["id"]);
            Expression argumentsKey = Expression.Constant(parameter.ParameterName);
            Expression property = Expression.Property(target, parameter.ParameterName);
            Expression value = Expression.Property(argumentsParameter, "Item", argumentsKey);
            Expression convertValueCall = Expression.Call(typeof(HttpParameterResolver), nameof(ConvertValue), new [] { typeof(object), parameter.ParameterType }, argumentsKey, value);
            Expression assign = Expression.Assign(property, convertValueCall);
            context.Statements.Add(assign);
        }

        private static void CollectBodyParameterAssignment(HttpParameterResolverCompilationContext context, Type bodyContract, HttpParameterInfo parameter, ParameterExpression argumentsParameter)
        {
            // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments, "someUdt1");
            Expression parameterName = Expression.Constant(parameter.ParameterName);
            Type[] typeArguments = { bodyContract, parameter.ParameterType, parameter.InputConverter };
            Expression addParameterFromBodyCall = Expression.Call(typeof(HttpParameterResolver), nameof(AddParameterFromBody), typeArguments, argumentsParameter, parameterName);
            context.Statements.Add(addParameterFromBodyCall);
        }

        private static void CollectBodyPropertyAssignment(HttpParameterResolverCompilationContext context, Type bodyContract, HttpParameterInfo parameter, Expression target, ParameterExpression argumentsParameter)
        {
            // input.someUdt1 = HttpParameterResolver.ConvertParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments);
            Expression property = Expression.Property(target, parameter.ParameterName);
            Type[] typeArguments = { bodyContract, parameter.ParameterType, parameter.InputConverter };
            Expression convertParameterFromBodyCall = Expression.Call(typeof(HttpParameterResolver), nameof(ConvertParameterFromBody), typeArguments, argumentsParameter);
            Expression assign = Expression.Assign(property, convertParameterFromBodyCall);
            context.Statements.Add(assign);
        }

        private static void CollectBodyParameterBindingAssignment(HttpParameterResolverCompilationContext context, Type bodyContract, Type binder, ParameterInfo parameter, Expression target, Expression argumentsParameter)
        {
            // HttpParameterResolver.BindParametersFromBody<SomeValueFromBody, SomeInputParameterClass, SomeInputBinder>(arguments, input);
            Type[] typeArguments = { bodyContract, parameter.ParameterType, binder };
            Expression bindParametersFromBodyCall = Expression.Call(typeof(HttpParameterResolver), nameof(BindParametersFromBody), typeArguments, argumentsParameter, target);
            context.Statements.Add(bindParametersFromBodyCall);
        }

        private static void CollectParameterAssignment(HttpParameterResolverCompilationContext context, string parameterName, Expression argumentsParameter, Expression value)
        {
            Expression key = Expression.Constant(parameterName);
            Expression valueCast = Expression.Convert(value, typeof(object));
            Expression addArgument = Expression.Call(argumentsParameter, nameof(ICollection<object>.Add), new Type[0], key, valueCast);
            context.Statements.Add(addArgument);
        }

        private static Expression CollectParameterValue(HttpParameterInfo parameter, IDictionary<string, HttpParameterSourceExpression> sources)
        {
            Expression value;

            switch (parameter.SourceKind)
            {
                case HttpParameterSourceKind.ConstantValue:
                    value = Expression.Constant(parameter.Value);
                    break;

                case HttpParameterSourceKind.SourceInstance:
                case HttpParameterSourceKind.SourceProperty:
                    HttpParameterSourceExpression source = sources[parameter.Source.Name];
                    value = source.Variable;
                    if (parameter.SourceKind == HttpParameterSourceKind.SourceProperty)
                    {
                        MemberExpression sourcePropertyExpression = Expression.Property(value, parameter.Source.PropertyName);
                        if (parameter.Items != null)
                            value = CollectItemsParameterValue(parameter, sourcePropertyExpression, sources);
                        else
                        {
                            value = sourcePropertyExpression;

                            if (parameter.Converter != null)
                                value = parameter.Converter.ConvertValue(value);
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Value of parameter '{parameter.ParameterName}' could not be resolved");
            }

            if (value.Type != parameter.ParameterType)
                value = Expression.Call(typeof(HttpParameterResolver), nameof(ConvertValue), new[] { value.Type, parameter.ParameterType }, Expression.Constant(parameter.ParameterName), value);

            return value;
        }

        private static Expression CollectItemsParameterValue(HttpParameterInfo parameter, MemberExpression sourcePropertyExpression, IDictionary<string, HttpParameterSourceExpression> sources)
        {
            Type itemType = GetItemType(sourcePropertyExpression.Type);
            MethodInfo structuredTypeFactoryMethod = GetStructuredTypeFactoryMethod(parameter.ParameterType, itemType);
            IDictionary<string, Expression> addMethodParameterValues = parameter.Items.AddItemMethod.GetParameters().ToDictionary(x => x.Name, x => (Expression)null);
            IDictionary<string, Type> addMethodParameterTypes = parameter.Items.AddItemMethod.GetParameters().ToDictionary(x => x.Name, x => x.ParameterType);

            ParameterExpression addItemFuncSetParameter = Expression.Parameter(parameter.ParameterType, "x");
            ParameterExpression addItemFuncItemParameter = Expression.Parameter(itemType, "y");
            ParameterExpression addItemFuncIndexParameter = Expression.Parameter(typeof(int), "i");

            foreach (HttpParameterInfo itemSource in parameter.Items.ParameterSources)
            {
                HttpParameterInfo itemParameter;
                ParameterExpression itemSourceVariable;
                if (itemSource.SourceKind == HttpParameterSourceKind.SourceProperty && itemSource.Source.PropertyName == ItemIndexPropertyName) // ITEM.$INDEX => i
                {
                    itemParameter = HttpParameterInfo.SourceInstance(itemSource.ParameterType, itemSource.ParameterName, null, ItemSourceName);
                    itemSourceVariable = addItemFuncIndexParameter;
                }
                else
                {
                    itemParameter = itemSource;
                    itemSourceVariable = addItemFuncItemParameter;
                }

                sources[ItemSourceName] = new HttpParameterSourceExpression(itemSourceVariable, null);
                addMethodParameterValues[itemSource.ParameterName] = CollectParameterValue(itemParameter, sources);
            }

            foreach (KeyValuePair<string, Expression> addMethodParameter in addMethodParameterValues.Where(x => x.Value == null).ToArray())
            {
                PropertyInfo property = itemType.GetTypeInfo().GetProperty(addMethodParameter.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null)
                    throw new InvalidOperationException($@"Can not map UDT column: {parameter.Items.UdtName ?? parameter.ParameterType.ToString()}.{addMethodParameter.Key}
Either create a mapping or make sure a property of the same name exists in the source: {parameter.Source.Name}.{parameter.Source.PropertyName}");

                Expression source = Expression.Property(addItemFuncItemParameter, property);
                Type targetType = addMethodParameterTypes[addMethodParameter.Key];
                if (targetType != source.Type)
                    source = Expression.Call(typeof(HttpParameterResolver), nameof(ConvertValue), new[] { source.Type, targetType }, Expression.Constant(addMethodParameter.Key), source);

                addMethodParameterValues[addMethodParameter.Key] = source;
            }

            MethodCallExpression addItemCall = Expression.Call(addItemFuncSetParameter, parameter.Items.AddItemMethod, addMethodParameterValues.Values);
            Expression addItemLambda = Expression.Lambda(addItemCall, addItemFuncSetParameter, addItemFuncItemParameter, addItemFuncIndexParameter);
            Expression value = Expression.Call(null, structuredTypeFactoryMethod, sourcePropertyExpression, addItemLambda);
            return value;
        }

        private static PropertyAccessor BuildDebugViewAccessor()
        {
            PropertyInfo property = typeof(Expression).GetTypeInfo().GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            return PropertyAccessor.Create(property);
        }

        private static ResolveParameters Compile(HttpParameterResolverCompilationContext context, out string source)
        {
            Expression block = Expression.Block(context.Variables, context.Statements);
            Expression<ResolveParameters> lambda = Expression.Lambda<ResolveParameters>(block, context.Parameters);
            ResolveParameters compiled = lambda.Compile();
            source = (string)DebugViewAccessor.Value.GetValue(lambda);
            return compiled;
        }

        private static void CollectExpectedParameters(HttpParameterResolutionMethod method, IEnumerable<HttpParameterInfo> parameters, Type bodyContract)
        {
            if (bodyContract != null)
                method.AddParameter(HttpParameterName.Body, bodyContract, false);

            foreach (HttpParameterInfo parameter in parameters.Where(x => x.IsUserParameter)) 
                method.AddParameter(parameter.ParameterName, parameter.ParameterType, parameter.IsOptional);
        }

        private static void AddParameterFromBody<TSource, TTarget, TConverter>(IDictionary<string, object> arguments, string parameterName) where TConverter : IFormattedInputConverter<TSource, TTarget>, new()
        {
            TTarget target = ConvertParameterFromBody<TSource, TTarget, TConverter>(arguments);
            arguments.Add(parameterName, target);
        }

        private static void BindParametersFromBody<TSource, TTarget, TBinder>(IDictionary<string, object> arguments, TTarget target) where TBinder : IFormattedInputBinder<TSource, TTarget>, new()
        {
            TBinder binder = new TBinder();
            TSource source = HttpParameterResolverUtility.ReadBody<TSource>(arguments);
            binder.Bind(source, target);
        }

        private static TTarget ConvertParameterFromBody<TSource, TTarget, TConverter>(IDictionary<string, object> arguments) where TConverter : IFormattedInputConverter<TSource, TTarget>, new()
        {
            TSource source = HttpParameterResolverUtility.ReadBody<TSource>(arguments);
            TConverter converter = new TConverter();
            TTarget target = converter.Convert(source);
            return target;
        }

        private static TTarget ConvertValue<TSource, TTarget>(string parameterName, TSource value)
        {
            try
            {
                object result = Convert.ChangeType(value, typeof(TTarget));
                return (TTarget)result;
            }
            catch (Exception exception)
            {
                throw CreateException("Parameter mapping failed", exception, null, parameterName);
            }
        }

        private static Exception CreateException(string message, Exception innerException, HttpActionDefinition action, string parameterName)
        {
            StringBuilder sb = new StringBuilder(message);
            if (action != null)
            {
                sb.AppendLine()
                  .Append("at ")
                  .Append(action.Method.ToString().ToUpperInvariant())
                  .Append(' ')
                  .Append(action.ComputedUri);
            }

            if (parameterName != null)
            {
                sb.AppendLine()
                  .Append("Parameter: ")
                  .Append(parameterName);
            }

            return new InvalidOperationException(sb.ToString(), innerException);
        }

        private static MethodInfo GetStructuredTypeFactoryMethod(Type implementationType, Type itemType)
        {
            foreach (MethodInfo method in typeof(StructuredType<>).MakeGenericType(implementationType).GetRuntimeMethods())
            {
                if (method.Name != "From")
                    continue;

                IList<ParameterInfo> parameters = method.GetParameters();
                if (parameters.Count != 2)
                    continue;

                if (parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                 && parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<,,>))
                {
                    return method.MakeGenericMethod(itemType);
                }
            }
            throw new InvalidOperationException("Could not find structured type factory method 'StructuredType<>.From()'");
        }

        private static Type GetItemType(Type enumerableType)
        {
            if (enumerableType.GetTypeInfo().GetInterfaces().All(x => x.GetGenericTypeDefinition() != typeof(IEnumerable<>)))
                throw new InvalidOperationException($"Type does not implement IEnumerable<>: {enumerableType}");

            return enumerableType.GenericTypeArguments[0];
        }
        #endregion

        #region Nested Types
        private sealed class HttpParameterResolverCompilationContext
        {
            public ICollection<ParameterExpression> Parameters { get; }
            public ICollection<ParameterExpression> Variables { get; }
            public ICollection<Expression> Statements { get; }
            public string LastVisitedParameter { get; set; }

            public HttpParameterResolverCompilationContext()
            {
                this.Parameters = new Collection<ParameterExpression>();
                this.Variables = new Collection<ParameterExpression>();
                this.Statements = new Collection<Expression>();
            }
        }

        private sealed class HttpParameterSourceExpression
        {
            public ParameterExpression Variable { get; }
            public Expression Assignment { get; }

            public HttpParameterSourceExpression(ParameterExpression variable, Expression assignment)
            {
                this.Variable = variable;
                this.Assignment = assignment;
            }
        }

        private sealed class HttpParameterInfo
        {
            public bool IsUserParameter { get; }               // Query parameter or body
            public HttpParameterSourceKind SourceKind { get; } 
            public ParameterInfo ContractParameter { get; }    // InputParameterClass
            public Type ParameterType { get; }                 
            public string ParameterName { get; }               
            public object Value { get; }
            public bool IsOptional { get; }
            public Type InputConverter { get; }                // IFormattedInputConverter<TSource, TTarget>
            public IHttpParameterConverter Converter { get; }  // CONVERT(value)
            public HttpParameterSourceInfo Source { get; }     // HLSESSION.LocaleId / databaseAccessorFactory
            public HttpItemsParameterInfo Items { get; }       // udt_columnname = ITEM.SomePropertyName

            private HttpParameterInfo(bool isUserParameter, HttpParameterSourceKind sourceKind, ParameterInfo contractParameter, Type parameterType, string parameterName, object value, bool isOptional, Type inputConverter, HttpParameterSourceInfo source, IHttpParameterConverter converter, HttpItemsParameterInfo items)
            {
                this.IsUserParameter = isUserParameter;
                this.SourceKind = sourceKind;
                this.ContractParameter = contractParameter;
                this.ParameterType = parameterType;
                this.ParameterName = parameterName;
                this.Value = value;
                this.IsOptional = isOptional;
                this.InputConverter = inputConverter;
                this.Source = source;
                this.Converter = converter;
                this.Items = items;
            }

            public static HttpParameterInfo Uri(ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional) => new HttpParameterInfo(true, HttpParameterSourceKind.Uri, contractParameter, parameterType, parameterName, null, isOptional, null, null, null, null);
            public static HttpParameterInfo SourceInstance(Type parameterType, string parameterName, IHttpParameterSourceProvider sourceProvider, string sourceName) => new HttpParameterInfo(false, HttpParameterSourceKind.SourceInstance, null, parameterType, parameterName, null, false, null, new HttpParameterSourceInfo(sourceName, sourceProvider), null, null);
            public static HttpParameterInfo SourceProperty(ParameterInfo contractParameter, Type parameterType, string parameterName, string sourceName, IHttpParameterSourceProvider sourceProvider, string sourcePropertyName, string converterName, IHttpParameterConverter converter) => new HttpParameterInfo(false, HttpParameterSourceKind.SourceProperty, contractParameter, parameterType, parameterName, null, false, null, new HttpParameterSourceInfo(sourceName, sourceProvider, sourcePropertyName), converter, null);
            public static HttpParameterInfo SourcePropertyItemsSource(ParameterInfo contractParameter, Type parameterType, string parameterName, IHttpParameterSourceProvider sourceProvider, string sourceName, string sourcePropertyName, string udtName, MethodInfo addItemMethod, IEnumerable<HttpParameterInfo> itemSources) => new HttpParameterInfo(false, HttpParameterSourceKind.SourceProperty, contractParameter, parameterType, parameterName, null, false, null, new HttpParameterSourceInfo(sourceName, sourceProvider, sourcePropertyName), null, new HttpItemsParameterInfo(udtName, addItemMethod, itemSources));
            public static HttpParameterInfo ConstantValue(ParameterInfo contractParameter, Type parameterType, string parameterName, object value) => new HttpParameterInfo(false, HttpParameterSourceKind.ConstantValue, contractParameter, parameterType, parameterName, value, false, null, null, null, null);
            public static HttpParameterInfo Body(ParameterInfo contractParameter, Type parameterType, string parameterName, Type inputConverter = null) => new HttpParameterInfo(false, HttpParameterSourceKind.Body, contractParameter, parameterType, parameterName, null, false, inputConverter, null, null, null);
        }

        private enum HttpParameterSourceKind
        {
            None,
            Uri,
            SourceInstance,
            SourceProperty,
            ConstantValue,
            Body
        }

        private sealed class HttpParameterSourceInfo
        {
            public string Name { get; }
            public IHttpParameterSourceProvider Instance { get; }
            public string PropertyName { get; }

            public HttpParameterSourceInfo(string name, IHttpParameterSourceProvider instance, string propertyName = null)
            {
                this.Name = name;
                this.Instance = instance;
                this.PropertyName = propertyName;
            }
        }

        private sealed class HttpItemsParameterInfo
        {
            public string UdtName { get; }
            public MethodInfo AddItemMethod { get; }
            public ICollection<HttpParameterInfo> ParameterSources { get; }

            public HttpItemsParameterInfo(string udtName, MethodInfo addItemMethod, IEnumerable<HttpParameterInfo> parameterSources)
            {
                this.ParameterSources = new Collection<HttpParameterInfo>();
                this.UdtName = udtName;
                this.AddItemMethod = addItemMethod;
                this.ParameterSources.AddRange(parameterSources);
            }
        }

        private sealed class HttpParameterResolutionMethod : IHttpParameterResolutionMethod
        {
            private readonly ResolveParameters _compiled;

            public string Source { get; }
            public IDictionary<string, HttpActionParameter> Parameters { get; }

            public HttpParameterResolutionMethod(string source, ResolveParameters compiled)
            {
                this._compiled = compiled;
                this.Source = source;
                this.Parameters = new Dictionary<string, HttpActionParameter>();
            }

            public void AddParameter(string name, Type type, bool isOptional) => this.Parameters.Add(name, new HttpActionParameter(name, type, isOptional));

            public void PrepareParameters(HttpRequestMessage request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver)
            {
                this._compiled(request, arguments, dependencyResolver);
            }
        }
        #endregion
    }
}