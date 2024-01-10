using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Dibix.Http.Server
{
    internal static class HttpParameterResolver
    {
        #region Fields
        private static readonly Type[] KnownDependencies = { typeof(IDatabaseAccessorFactory) };
        private static readonly Lazy<PropertyAccessor> DebugViewAccessor = new Lazy<PropertyAccessor>(BuildDebugViewAccessor);
        private static readonly string ItemSourceName = ItemParameterSource.SourceName;
        private const string SelfPropertyName = "$SELF";
        #endregion

        #region Delegates
        private delegate void ResolveParameters(IHttpRequestDescriptor request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver, IHttpActionDescriptor action);
        #endregion

        #region Public Methods
        public static IHttpParameterResolutionMethod Compile(IHttpActionDescriptor action)
        {
            CompilationContext compilationContext = new CompilationContext();
            try
            {
                // (HttpRequestDescriptor request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver, IHttpActionDescriptor action) => 
                ParameterExpression requestParameter = Expression.Parameter(typeof(IHttpRequestDescriptor), "request");
                ParameterExpression argumentsParameter = Expression.Parameter(typeof(IDictionary<string, object>), "arguments");
                ParameterExpression dependencyResolverParameter = Expression.Parameter(typeof(IParameterDependencyResolver), "dependencyResolver");
                ParameterExpression actionParameter = Expression.Parameter(typeof(IHttpActionDescriptor), "action");
                compilationContext.Parameters.Add(requestParameter);
                compilationContext.Parameters.Add(argumentsParameter);
                compilationContext.Parameters.Add(dependencyResolverParameter);
                compilationContext.Parameters.Add(actionParameter);
                
                IDictionary<string, Expression> sourceMap = new Dictionary<string, Expression>();
                ICollection<HttpParameterInfo> @params = CollectParameters(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap).ToArray();

                // IDatabaseAccesorFactory databaseAccesorFactory = dependencyResolver.Resolve<IDatabaseAccesorFactory>();
                // IActionAuthorizationContext actionAuthorizationContext = dependencyResolver.Resolve<IActionAuthorizationContext>();
                // ...
                CollectParameterSources(@params);

                // SomeValueFromBody body = HttpParameterResolver.ReadBody<SomeValueFromBody>(arguments);
                //
                // arguments["databaseAccessorFactory"] = databaseAccessorFactory;
                // arguments["lcid"] = actionAuthorizationContext.LocaleId;
                // arguments["id"] = body.Id;
                //
                // SomeInputParameterClass input = new SomeInputParameterClass();
                // input.lcid = actionAuthorizationContext.LocaleId;
                // input.id = HttpParameterResolver.ConvertValue<object, int>("id", arguments["id"], action);
                // HttpParameterResolver.BindParametersFromBody<SomeValueFromBody, SomeInputParameterClass, SomeInputBinder>(arguments, input, "input");
                // arguments["input"] = input;
                //
                // input.someUdt1 = HttpParameterResolver.ConvertParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments);
                // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments, "someUdt1");
                // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt2, SomeUdt2InputConverter>(arguments, "someUdt2");
                // ...
                CollectParameterAssignments(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, @params);

                ResolveParameters compiled = Compile(compilationContext, out string source);
                HttpParameterResolutionMethod method = new HttpParameterResolutionMethod(action, source, compiled);
                CollectApiParameters(method, @params, action.BodyContract);
                return method;
            }
            catch (Exception exception)
            {
                throw CreateException("Http parameter resolver compilation failed", exception, action, compilationContext.LastVisitedParameter);
            }
        }
        #endregion

        #region Private Methods
        private static IEnumerable<HttpParameterInfo> CollectParameters(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap)
        {
            HashSet<string> pathParameters = new HashSet<string>();
            if (!String.IsNullOrEmpty(action.ChildRoute))
                pathParameters.AddRange(HttpParameterUtility.ExtractPathParameters(action.ChildRoute).Select(x => x.Value));

            foreach (ParameterInfo parameter in action.Target.GetParameters())
            {
                compilationContext.LastVisitedParameter = parameter.Name;

                // We don't support out parameters in REST APIs, so we assume that this method is used as both backend accessor and REST API.
                // Therefore we just skip it
                if (parameter.IsOut)
                    continue;

                if (KnownDependencies.Contains(parameter.ParameterType))
                {
                    string sourceName = parameter.ParameterType.Name.TrimStart('I');
                    sourceName = String.Concat(Char.ToLowerInvariant(sourceName[0]), sourceName.Substring(1));
                    IHttpParameterSourceProvider sourceProvider = new HttpParameterDependencySourceProvider(parameter.ParameterType);
                    HttpParameterSourceInfo source = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, sourceName, sourceProvider, propertyPath: null);
                    yield return HttpParameterInfo.SourceInstance(parameter.ParameterType, parameter.Name, source);
                    continue;
                }

                if (parameter.IsDefined(typeof(InputClassAttribute)))
                {
                    foreach (PropertyInfo property in parameter.ParameterType.GetRuntimeProperties())
                    {
                        if (action.TryGetParameter(property.Name, out HttpParameterSource parameterSource))
                        {
                            yield return CollectExplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameterSource, parameter, property.PropertyType, property.Name, isOptional: false, defaultValue: null);
                        }
                        else if (action.BodyBinder != null)
                        {
                            yield return HttpParameterInfo.Body(parameter, property.PropertyType, property.Name);
                        }
                        else
                        {
                            yield return CollectImplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, pathParameters, sourceMap, parameter, property.PropertyType, property.Name, false);
                        }
                    }
                }
                else
                {
                    if (action.TryGetParameter(parameter.Name, out HttpParameterSource parameterSource))
                    {
                        yield return CollectExplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameterSource, contractParameter: null, parameter.ParameterType, parameter.Name, parameter.IsOptional, parameter.DefaultValue);
                    }
                    else if (action.BodyBinder != null)
                    {
                        throw new InvalidOperationException($"Using a binder for the body is only supported if the target parameter is a class and is marked with the {typeof(InputClassAttribute)}");
                    }
                    else
                    {
                        yield return CollectImplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, pathParameters, sourceMap, contractParameter: null, parameter.ParameterType, parameter.Name, parameter.IsOptional, parameter.DefaultValue);
                    }
                }
            }

            compilationContext.LastVisitedParameter = null;
        }

        private static HttpParameterInfo CollectExplicitParameter(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterSource source, ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional, object defaultValue)
        {
            switch (source)
            {
                case HttpParameterPropertySource propertySource:
                    IHttpParameterSourceProvider sourceProvider = GetSourceProvider(propertySource.SourceName);
                    bool isItemsParameter = typeof(StructuredType).IsAssignableFrom(parameterType);
                    if (!isItemsParameter)
                    {
                        IHttpParameterConverter converter = null;
                        if (!String.IsNullOrEmpty(propertySource.ConverterName))
                            converter = GetConverter(propertySource.ConverterName);

                        HttpParameterSourceInfo sourceInfo = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, propertySource.SourceName, sourceProvider, propertySource.PropertyPath);
                        return HttpParameterInfo.SourceProperty(contractParameter, parameterType, parameterName, isOptional, defaultValue, sourceInfo, converter);
                    }

                    return CollectItemsParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, contractParameter, parameterType, parameterName, isOptional, defaultValue, propertySource, sourceProvider);

                case HttpParameterBodySource bodySource:
                    Type inputConverter = Type.GetType(bodySource.ConverterName, true);
                    return HttpParameterInfo.Body(contractParameter, parameterType, parameterName, inputConverter);

                case HttpParameterConstantSource constantSource:
                    return HttpParameterInfo.ConstantValue(contractParameter, parameterType, parameterName, constantSource.Value);

                default:
                    throw new InvalidOperationException($"Unsupported parameter source type: '{source.GetType()}'");
            }
        }

        private static HttpParameterInfo CollectImplicitParameter(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, ICollection<string> pathParameters, IDictionary<string, Expression> sourceMap, ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional) => CollectImplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, pathParameters, sourceMap, contractParameter, parameterType, parameterName, isOptional, null);
        private static HttpParameterInfo CollectImplicitParameter(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, ICollection<string> pathParameters, IDictionary<string, Expression> sourceMap, ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional, object defaultValue)
        {
            if (pathParameters.Contains(parameterName, StringComparer.OrdinalIgnoreCase))
                return HttpParameterInfo.Path(contractParameter, parameterType, parameterName, isOptional, defaultValue);

            if (action.BodyContract == null)
                return HttpParameterInfo.Query(contractParameter, parameterType, parameterName, isOptional, defaultValue);

            string sourcePropertyName = parameterName;
            PropertyInfo sourceProperty = action.BodyContract.GetProperty(sourcePropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (sourceProperty == null)
                return HttpParameterInfo.Query(contractParameter, parameterType, parameterName, isOptional, defaultValue);

            sourcePropertyName = sourceProperty.Name;
            IHttpParameterSourceProvider sourceProvider = GetSourceProvider(BodyParameterSourceProvider.SourceName);

            bool isItemsParameter = typeof(StructuredType).IsAssignableFrom(parameterType);
            if (isItemsParameter)
                return CollectItemsParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, contractParameter, parameterType, parameterName, isOptional, defaultValue, new HttpParameterPropertySource(BodyParameterSourceProvider.SourceName, sourcePropertyName, null), sourceProvider);

            HttpParameterSourceInfo sourceInfo = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, BodyParameterSourceProvider.SourceName, sourceProvider, propertyPath: sourcePropertyName);
            return HttpParameterInfo.SourceProperty(contractParameter, parameterType, parameterName, isOptional, defaultValue, sourceInfo, converter: null);
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

        private static HttpParameterInfo CollectItemsParameter(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional, object defaultValue, HttpParameterPropertySource propertySource, IHttpParameterSourceProvider sourceProvider)
        {
            string udtName = parameterType.GetCustomAttribute<StructuredTypeAttribute>()?.UdtName;
            MethodInfo addMethod = parameterType.SafeGetMethod("Add");
            IDictionary<string, Type> parameterMap = addMethod.GetParameters().ToDictionary(x => x.Name, x => x.ParameterType);
            IEnumerable<HttpParameterInfo> itemSources = propertySource.ItemSources.Select(x =>
            {
                if (!parameterMap.TryGetValue(x.Key, out Type itemParameterType))
                    throw new InvalidOperationException($"Target name does not match a UDT column: {udtName ?? parameterType.ToString()}.{x.Key}");

                return CollectExplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, x.Value, null, itemParameterType, x.Key, isOptional, defaultValue);
            });
            HttpParameterSourceInfo source = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, propertySource.SourceName, sourceProvider, propertySource.PropertyPath);
            return HttpParameterInfo.SourcePropertyItemsSource(contractParameter, parameterType, parameterName, source, udtName, addMethod, itemSources);
        }

        private static void CollectParameterSources(IEnumerable<HttpParameterInfo> @params)
        {
            foreach (HttpParameterInfo parameter in @params)
                parameter.Source?.CollectSourceInstance();
        }

        private static void CollectParameterAssignments(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, IEnumerable<HttpParameterInfo> parameters)
        {
            foreach (IGrouping<ParameterInfo, HttpParameterInfo> parameterGroup in parameters.GroupBy(x => x.ContractParameter))
            {
                ParameterExpression contractParameterVariable = null;
                if (parameterGroup.Key != null)
                {
                    compilationContext.LastVisitedParameter = parameterGroup.Key.Name;

                    // SomeInputParameterClass input = new SomeInputParameterClass();
                    contractParameterVariable = CollectInputClass(compilationContext, parameterGroup.Key);
                }

                foreach (HttpParameterInfo parameter in parameterGroup)
                {
                    compilationContext.LastVisitedParameter = parameter.InternalParameterName;

                    if (contractParameterVariable == null)
                    {
                        if (parameter.SourceKind == HttpParameterSourceKind.Body)
                        {
                            // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments, "someUdt1");
                            CollectBodyParameterAssignment(compilationContext, action.SafeGetBodyContract(), parameter, argumentsParameter);
                        }
                        else if (IsUri(parameter.Location))
                        {
                            // arguments["lcid] = arguments["localeid"];
                            // if (arguments.TryGetvalue("lcid", out object value) && value == null)
                            // {
                            //     arguments["lcid"] = 5; // Default value
                            // }
                            // arguments["lcid"] = CONVERT(arguments["lcid"])
                            CollectUriParameterAssignment(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter);
                        }
                        else
                        {
                            // arguments[lcid"] = actionAuthorizationContext.LocaleId;
                            CollectNonUserParameterAssignment(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter);
                        }
                    }
                    else
                    {
                        if (parameter.SourceKind == HttpParameterSourceKind.Body)
                        {
                            if (parameter.InputConverter != null)
                            {
                                // input.someUdt1 = HttpParameterResolver.ConvertParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments);
                                CollectBodyPropertyAssignment(compilationContext, action.SafeGetBodyContract(), parameter, contractParameterVariable, argumentsParameter);
                            }
                        }
                        else if (IsUri(parameter.Location))
                        {
                            // input.id = HttpParameterResolver.ConvertValue<object, int>("id", arguments["id"], action);
                            CollectUriPropertyAssignment(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter, contractParameterVariable);
                        }
                        else
                        {
                            // input.lcid = actionAuthorizationContext.LocaleId;
                            CollectNonUserPropertyAssignment(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter, contractParameterVariable);
                        }
                    }
                }

                if (contractParameterVariable != null)
                {
                    compilationContext.LastVisitedParameter = parameterGroup.Key.Name;

                    if (action.BodyBinder != null)
                    {
                        // HttpParameterResolver.BindParametersFromBody<SomeValueFromBody, SomeInputParameterClass, SomeInputBinder>(arguments, input);
                        CollectBodyParameterBindingAssignment(compilationContext, action.SafeGetBodyContract(), action.BodyBinder, parameterGroup.Key, contractParameterVariable, argumentsParameter);
                    }

                    // arguments["input"] = input;
                    CollectParameterAssignment(compilationContext, contractParameterVariable.Name, argumentsParameter, contractParameterVariable);
                }
            }

            compilationContext.LastVisitedParameter = null;
        }

        private static ParameterExpression CollectInputClass(CompilationContext compilationContext, ParameterInfo parameter)
        {
            ParameterExpression complexParameterVariable = Expression.Variable(parameter.ParameterType, parameter.Name);
            Expression complexParameterInstance = Expression.New(parameter.ParameterType);
            Expression complexParameterAssign = Expression.Assign(complexParameterVariable, complexParameterInstance);
            compilationContext.Variables.Add(complexParameterVariable);
            compilationContext.Statements.Add(complexParameterAssign);
            return complexParameterVariable;
        }

        private static void CollectNonUserParameterAssignment(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter)
        {
            // arguments["lcid"] = actionAuthorizationContext.LocaleId;
            Expression value = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter);
            CollectParameterAssignment(compilationContext, parameter.InternalParameterName, argumentsParameter, value);
        }

        private static void CollectNonUserPropertyAssignment(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter, Expression contractParameterVariable)
        {
            // input.lcid = actionAuthorizationContext.LocaleId;
            Expression property = Expression.Property(contractParameterVariable, parameter.InternalParameterName);
            Expression value = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter);
            Expression assign = Expression.Assign(property, value); 
            compilationContext.Statements.Add(assign);
        }

        private static void CollectUriParameterAssignment(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter)
        {
            bool hasDefaultValue = parameter.DefaultValue != DBNull.Value && parameter.DefaultValue != null;

            if (parameter.SourceKind == HttpParameterSourceKind.SourceProperty) // QUERY or PATH source
            {
                // arguments["lcid] = arguments["localeid"];
                Expression value = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter, generateConverterStatement: !hasDefaultValue, ensureCorrectType: !hasDefaultValue);
                CollectParameterAssignment(compilationContext, parameter.InternalParameterName, argumentsParameter, value);
            }

            if (!hasDefaultValue) 
                return;
            
            // if (arguments.TryGetvalue("lcid", out object value) && value == null)
            // {
            //     arguments["lcid"] = 5; // Default value
            // }
            if (parameter.SourceKind == HttpParameterSourceKind.Query
             || parameter.SourceKind == HttpParameterSourceKind.SourceProperty
             && parameter.Source.SourceName == QueryParameterSourceProvider.SourceName)
                CollectQueryParameterDefaultValueAssignment(compilationContext, parameter, argumentsParameter);

            // arguments["lcid"] = CONVERT(arguments["lcid"])
            if (parameter.Converter != null)
            {
                Expression value = HttpParameterResolverUtility.BuildReadableArgumentAccessorExpression(argumentsParameter, parameter.InternalParameterName);
                value = CollectConverterStatement(parameter, value, action, requestParameter, dependencyResolverParameter, actionParameter);
                CollectParameterAssignment(compilationContext, parameter.InternalParameterName, argumentsParameter, value);
            }
        }

        private static void CollectQueryParameterDefaultValueAssignment(CompilationContext compilationContext, HttpParameterInfo parameter, Expression argumentsParameter)
        {
            // if (arguments.TryGetvalue("lcid", out object value) && value == null)
            // {
            //     arguments["lcid"] = 5; // Default value
            // }
            Expression argumentsKey = Expression.Constant(parameter.InternalParameterName);
            ParameterExpression defaultValue = Expression.Parameter(typeof(object), $"{parameter.InternalParameterName}DefaultValue");
            Expression tryGetValue = Expression.Call(argumentsParameter, nameof(IDictionary<object, object>.TryGetValue), Type.EmptyTypes, argumentsKey, defaultValue);
            Expression emptyParameterValue = Expression.Equal(defaultValue, Expression.Constant(null));
            Expression condition = Expression.And(tryGetValue, emptyParameterValue);
            Expression property = Expression.Property(argumentsParameter, "Item", argumentsKey);
            Expression value = Expression.Convert(Expression.Constant(parameter.DefaultValue), typeof(object));
            Expression assign = Expression.Assign(property, value);
            Expression @if = Expression.IfThen(condition, assign);
            compilationContext.Variables.Add(defaultValue);
            compilationContext.Statements.Add(@if);
        }

        private static void CollectUriPropertyAssignment(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter, Expression contractParameterVariable)
        {
            // input.id = CONVERT(HttpParameterResolver.ConvertValue<object, int>("id", arguments["id"], action));
            Expression property = Expression.Property(contractParameterVariable, parameter.InternalParameterName);
            Expression value = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter);
            Expression assign = Expression.Assign(property, value);
            compilationContext.Statements.Add(assign);
        }

        private static void CollectBodyParameterAssignment(CompilationContext compilationContext, Type bodyContract, HttpParameterInfo parameter, Expression argumentsParameter)
        {
            // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments, "someUdt1");
            Expression parameterName = Expression.Constant(parameter.InternalParameterName);
            Type[] typeArguments = { bodyContract, parameter.ParameterType, parameter.InputConverter };
            Expression addParameterFromBodyCall = Expression.Call(typeof(HttpParameterResolver), nameof(AddParameterFromBody), typeArguments, argumentsParameter, parameterName);
            compilationContext.Statements.Add(addParameterFromBodyCall);
        }

        private static void CollectBodyPropertyAssignment(CompilationContext compilationContext, Type bodyContract, HttpParameterInfo parameter, Expression contractParameterVariable, Expression argumentsParameter)
        {
            // input.someUdt1 = HttpParameterResolver.ConvertParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments);
            Expression property = Expression.Property(contractParameterVariable, parameter.InternalParameterName);
            Type[] typeArguments = { bodyContract, parameter.ParameterType, parameter.InputConverter };
            Expression convertParameterFromBodyCall = Expression.Call(typeof(HttpParameterResolver), nameof(ConvertParameterFromBody), typeArguments, argumentsParameter);
            Expression assign = Expression.Assign(property, convertParameterFromBodyCall);
            compilationContext.Statements.Add(assign);
        }

        private static void CollectBodyParameterBindingAssignment(CompilationContext compilationContext, Type bodyContract, Type binder, ParameterInfo parameter, Expression contractParameterVariable, Expression argumentsParameter)
        {
            // HttpParameterResolver.BindParametersFromBody<SomeValueFromBody, SomeInputParameterClass, SomeInputBinder>(arguments, input);
            Type[] typeArguments = { bodyContract, parameter.ParameterType, binder };
            Expression bindParametersFromBodyCall = Expression.Call(typeof(HttpParameterResolver), nameof(BindParametersFromBody), typeArguments, argumentsParameter, contractParameterVariable);
            compilationContext.Statements.Add(bindParametersFromBodyCall);
        }

        private static void CollectParameterAssignment(CompilationContext compilationContext, string parameterName, Expression argumentsParameter, Expression value)
        {
            Expression property = HttpParameterResolverUtility.BuildWritableArgumentAccessorExpression(argumentsParameter, parameterName);
            Expression valueCast = Expression.Convert(value, typeof(object));
            Expression assign = Expression.Assign(property, valueCast);
            compilationContext.Statements.Add(assign);
        }

        private static Expression CollectParameterValue(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter, bool generateConverterStatement = true, bool ensureCorrectType = true, bool ensureNullPropagation = false)
        {
            Expression value;

            switch (parameter.SourceKind)
            {
                case HttpParameterSourceKind.ConstantValue:
                    value = Expression.Constant(parameter.Value);
                    break;

                case HttpParameterSourceKind.Query:
                case HttpParameterSourceKind.Path:
                    value = HttpParameterResolverUtility.BuildReadableArgumentAccessorExpression(argumentsParameter, parameter.InternalParameterName);
                    break;

                case HttpParameterSourceKind.SourceInstance:
                case HttpParameterSourceKind.SourceProperty:
                    if (parameter.Source.Value != null)
                    {
                        // Use resolved static value from source provider
                        value = parameter.Source.Value;
                    }
                    else if (sourceMap.TryGetValue(parameter.Source.SourceName, out value))
                    {
                        // Expand property path on an existing source instance variable
                        if (parameter.SourceKind == HttpParameterSourceKind.SourceProperty)
                        {
                            value = CollectSourcePropertyValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter, value, ensureNullPropagation);
                        }
                    }
                    else
                        throw new InvalidOperationException($"Value of parameter '{parameter.InternalParameterName}' could not be resolved from source '{parameter.Source.SourceName}'");

                    break;

                default:
                    throw new InvalidOperationException($"Value of parameter '{parameter.InternalParameterName}' could not be resolved");
            }

            if (generateConverterStatement) 
                value = CollectConverterStatement(parameter, value, action, requestParameter, dependencyResolverParameter, actionParameter);

            // ResolveParameterFromNull
            if (parameter.SourceKind == HttpParameterSourceKind.ConstantValue && parameter.Value == null) 
                return value;

            if (ensureCorrectType)
                value = EnsureCorrectType(parameter.InternalParameterName, value, parameter.ParameterType, actionParameter);

            return value;
        }

        private static Expression CollectSourcePropertyValue(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter, Expression value, bool ensureNullPropagation)
        {
            return CollectSourcePropertyValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, parameter, value, ensureNullPropagation, parameter.Source.PropertyPath);
        }
        private static Expression CollectSourcePropertyValue(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter, Expression value, bool ensureNullPropagation, string propertyPath)
        {
            string[] parts = propertyPath.Split('.');
            ICollection<Expression> nullCheckTargets = new Collection<Expression>();
            for (int i = 0; i < parts.Length; i++)
            {
                string propertyName = parts[i];
                if (propertyName == SelfPropertyName)
                    continue;

                MemberExpression sourcePropertyExpression = Expression.Property(value, propertyName);
                value = parameter.Items != null ? CollectItemsParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, parameter, sourcePropertyExpression, sourceMap, ensureNullPropagation) : sourcePropertyExpression;

                if (ensureNullPropagation && i + 1 < parts.Length)
                    nullCheckTargets.Add(sourcePropertyExpression);
            }

            if (nullCheckTargets.Any())
            {
                Expression test = nullCheckTargets.Select(x => Expression.NotEqual(x, Expression.Constant(null))).Aggregate(Expression.AndAlso /* Short-circuit behavior like && in C# */);
                bool hasDefaultValue = parameter.DefaultValue != DBNull.Value && parameter.DefaultValue != null;
                Expression fallbackValue = hasDefaultValue ? (Expression)Expression.Constant(parameter.DefaultValue) : Expression.Default(parameter.ParameterType);
                value = EnsureCorrectType(parameter.InternalParameterName, value, parameter.ParameterType, actionParameter);
                value = Expression.Condition(test, value, fallbackValue);
            }

            return value;
        }

        private static Expression CollectItemsParameterValue
        (
            IHttpActionDescriptor action
          , Expression requestParameter
          , Expression argumentsParameter
          , Expression dependencyResolverParameter
          , Expression actionParameter
          , CompilationContext compilationContext
          , HttpParameterInfo parameter
          , Expression sourcePropertyExpression
          , IDictionary<string, Expression> sourceMap
          , bool ensureNullPropagation
        )
        {
            Type itemType = GetItemType(sourcePropertyExpression.Type);
            IDictionary<string, Expression> addMethodParameterValues = parameter.Items.AddItemMethod.GetParameters().ToDictionary(x => x.Name, x => (Expression)null);
            IDictionary<string, Type> addMethodParameterTypes = parameter.Items.AddItemMethod.GetParameters().ToDictionary(x => x.Name, x => x.ParameterType);

            ParameterExpression addItemFuncSetParameter = Expression.Parameter(parameter.ParameterType, "x");
            ParameterExpression addItemFuncItemParameter = Expression.Parameter(itemType, "y");
            ParameterExpression addItemFuncIndexParameter = null;

            foreach (HttpParameterInfo itemSource in parameter.Items.ParameterSources)
            {
                HttpParameterInfo itemParameter;
                ParameterExpression itemSourceVariable;
                if (itemSource.SourceKind == HttpParameterSourceKind.SourceProperty && itemSource.Source.PropertyPath == ItemParameterSource.IndexPropertyName) // ITEM.$INDEX => i
                {
                    HttpParameterSourceInfo source = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, ItemSourceName, sourceProvider: null, propertyPath: null);
                    itemParameter = HttpParameterInfo.SourceInstance(itemSource.ParameterType, itemSource.InternalParameterName, source);
                    if (addItemFuncIndexParameter == null)
                        addItemFuncIndexParameter = Expression.Parameter(typeof(int), "i"); // Lazy

                    itemSourceVariable = addItemFuncIndexParameter;
                }
                else
                {
                    itemParameter = itemSource;
                    itemSourceVariable = addItemFuncItemParameter;
                }

                sourceMap[ItemSourceName] = itemSourceVariable;
                addMethodParameterValues[itemSource.InternalParameterName] = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, actionParameter, compilationContext, sourceMap, itemParameter, ensureNullPropagation: ensureNullPropagation);
            }

            foreach (KeyValuePair<string, Expression> addMethodParameter in addMethodParameterValues.Where(x => x.Value == null).ToArray())
            {
                Type targetType = addMethodParameterTypes[addMethodParameter.Key];
                Expression source;

                PropertyInfo property = itemType.GetProperty(addMethodParameter.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (addMethodParameterValues.Count == 1 && targetType == itemType)
                {
                    // IEnumerable<string> => UDT(nvarchar)
                    source = addItemFuncItemParameter;
                }
                else if (property != null)
                {
                    // IEnumerable<Model> => Model.Property1 => UDT(property1, ...)
                    source = Expression.Property(addItemFuncItemParameter, property);
                }
                else
                {
                    throw new InvalidOperationException($@"Can not map UDT column: {parameter.Items.UdtName ?? parameter.ParameterType.ToString()}.{addMethodParameter.Key}
Either create a mapping or make sure a property of the same name exists in the source: {parameter.Source.SourceName}.{parameter.Source.PropertyPath}");
                }

                source = EnsureCorrectType(addMethodParameter.Key, source, targetType, actionParameter);
                addMethodParameterValues[addMethodParameter.Key] = source;
            }

            MethodCallExpression addItemCall = Expression.Call(addItemFuncSetParameter, parameter.Items.AddItemMethod, addMethodParameterValues.Values);
            ICollection<ParameterExpression> parameters = new Collection<ParameterExpression> { addItemFuncSetParameter, addItemFuncItemParameter };
            if (addItemFuncIndexParameter != null)
                parameters.Add(addItemFuncIndexParameter);

            Expression addItemLambda = Expression.Lambda(addItemCall, parameters);
            MethodInfo structuredTypeFactoryMethod = GetStructuredTypeFactoryMethod(parameter.ParameterType, itemType, withIndex: addItemFuncIndexParameter != null);
            Expression value = Expression.Call(null, structuredTypeFactoryMethod, sourcePropertyExpression, addItemLambda);
            return value;
        }

        private static Expression CollectConverterStatement(HttpParameterInfo parameter, Expression value, IHttpActionDescriptor action, Expression requestParameter, Expression dependencyResolverParameter, Expression actionParameter)
        {
            if (parameter.Converter == null)
                return value;

            value = EnsureCorrectType(parameter.InternalParameterName, value, parameter.Converter.ExpectedInputType, actionParameter);
            HttpParameterConversionContext context = new HttpParameterConversionContext(action, requestParameter, dependencyResolverParameter, actionParameter);
            value = parameter.Converter.ConvertValue(value, context);
            return value;
        }

        private static Expression EnsureCorrectType(string parameterName, Expression valueExpression, Type targetType, Expression actionParameter)
        {
            if (valueExpression.Type == targetType)
                return valueExpression;

            // Don't generate unnecessary convert calls
            if (targetType == typeof(object))
                return valueExpression;

            if (TryStructuredTypeConversion(parameterName, valueExpression, targetType, out Expression newValueExpression))
                return newValueExpression;

            Expression convertCall = Expression.Call(typeof(HttpParameterResolver), nameof(ConvertValue), new[] { valueExpression.Type, targetType }, Expression.Constant(parameterName), valueExpression, actionParameter);
            return convertCall;
        }

        private static bool TryStructuredTypeConversion(string parameterName, Expression valueExpression, Type targetType, out Expression newValueExpression)
        {
            Type sourceType = valueExpression.Type;
            bool isSourceEnumerable = Enumerable.Repeat(sourceType, 1)
                                                .Union(sourceType.GetInterfaces())
                                                .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (!isSourceEnumerable || !typeof(StructuredType).IsAssignableFrom(targetType))
            {
                newValueExpression = null;
                return false;
            }

            MethodInfo addMethod = GetStructuredTypeAddMethod(targetType);
            ParameterInfo[] parameters = addMethod.GetParameters();
            if (parameters.Length != 1)
                throw new NotSupportedException($"{nameof(StructuredType)} implementations with {parameters.Length} parameters are not supported");

            newValueExpression = BuildSingleValueStructuredTypeConversion(parameterName, targetType, addMethod, valueExpression);
            return true;
        }

        private static Expression BuildSingleValueStructuredTypeConversion(string parameterName, Type type, MethodInfo addMethod, Expression valueExpression)
        {
            // IdSet structuredType = new IdSet();
            ParameterExpression structureTypeVariable = Expression.Variable(type, parameterName);
            Expression structuredTypeValue = Expression.New(type);
            Expression structuredTypeAssign = Expression.Assign(structureTypeVariable, structuredTypeValue);

            // IEnumerator<int> valuesEnumerator;
            // try
            // {
            //     valuesEnumerator = values.GetEnumerator();
            //     while (valuesEnumerator.MoveNext())
            //     {
            //         structuredType.Add(structuredTypeEnumerator.Current);
            //     }
            // }
            // finally
            // {
            //     if (valuesEnumerator != null)
            //         valuesEnumerator.Dispose();
            // }
            Type parameterType = addMethod.GetParameters().Select(x => x.ParameterType).Single();
            ExpressionUtility.Foreach
            (
                parameterName
              , valueExpression
              , parameterType
              , builder => builder.AddStatement(Expression.Call(structureTypeVariable, addMethod, builder.Element))
              , out ParameterExpression enumeratorVariable
              , out Expression enumeratorStatement
            );

            Expression block = Expression.Block(EnumerableExtensions.Create(structureTypeVariable, enumeratorVariable), structuredTypeAssign, enumeratorStatement, structureTypeVariable);
            return block;
        }

        private static MethodInfo GetStructuredTypeAddMethod(Type type)
        {
            MethodInfo addMethod = type.SafeGetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
            return addMethod;
        }

        private static PropertyAccessor BuildDebugViewAccessor()
        {
            PropertyInfo property = typeof(Expression).GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic);
            return PropertyAccessor.Create(property);
        }

        private static ResolveParameters Compile(CompilationContext compilationContext, out string source)
        {
            Expression block = Expression.Block(compilationContext.Variables, compilationContext.Statements);
            Expression<ResolveParameters> lambda = Expression.Lambda<ResolveParameters>(block, compilationContext.Parameters);
            ResolveParameters compiled = lambda.Compile();
            source = (string)DebugViewAccessor.Value.GetValue(lambda);
            return compiled;
        }

        private static void CollectApiParameters(HttpParameterResolutionMethod method, ICollection<HttpParameterInfo> parameters, Type bodyContract)
        {
            if (bodyContract != null)
            {
                // If the target method does not expect a parameter of the body type, we have to add a pseudo parameter
                // That way ASP.NET will use a formatter binding and read the body for us, which we can later read from the arguments dictionary.
                if (parameters.All(x => x.ParameterType != bodyContract))
                    method.AddParameter(HttpParameterName.Body, bodyContract, HttpParameterLocation.NonUser, isOptional: false);
            }

            foreach (HttpParameterInfo parameter in parameters.Where(x => x.Location != HttpParameterLocation.NonUser).DistinctBy(x => x.ApiParameterName)) 
                method.AddParameter(parameter.ApiParameterName, parameter.ParameterType, parameter.Location, parameter.IsOptional);
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

        private static TTarget ConvertValue<TSource, TTarget>(string parameterName, TSource value, IHttpActionDescriptor action)
        {
            try
            {
                bool valueIsNull = Equals(value, null);
                object result = null;
                //if (valueIsNull)
                {
                    TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(TTarget));
                    if (value is TTarget)
                    {
                        result = value;
                    }
                    else if (typeConverter.CanConvertFrom(typeof(TSource)))
                    {
                        result = typeConverter.ConvertFrom(value);
                    }
                    else if (typeof(TTarget) == typeof(string) && !valueIsNull)
                    {
                        result = value.ToString();
                    }
                    else
                    {
                        Type targetType = typeof(TTarget);
                        Type nullableType = Nullable.GetUnderlyingType(targetType);
                        bool isNullableValueType = nullableType != null;
                        if (isNullableValueType)
                            targetType = nullableType;

                        if (!isNullableValueType || !valueIsNull)
                            result = Convert.ChangeType(value, targetType);
                    }
                }
                return (TTarget)result;
            }
            catch (Exception exception)
            {
                throw CreateException("Parameter mapping failed", exception, action, parameterName);
            }
        }

        private static bool IsUri(HttpParameterLocation location) => location == HttpParameterLocation.Query || location == HttpParameterLocation.Path;

        private static Exception CreateException(string message, Exception innerException, IHttpActionDescriptor action, string parameterName)
        {
            StringBuilder sb = new StringBuilder(message);
            if (action != null)
            {
                sb.AppendLine()
                  .Append("at ")
                  .Append(action.Method.ToString().ToUpperInvariant())
                  .Append(' ')
                  .Append(action.Uri);
            }

            if (parameterName != null)
            {
                sb.AppendLine()
                  .Append("Parameter: ")
                  .Append(parameterName);

                if (action != null && action.TryGetParameter(parameterName, out HttpParameterSource source))
                {
                    sb.AppendLine()
                      .Append("Source: ")
                      .Append(source.Description);
                }
            }

            return new InvalidOperationException(sb.ToString(), innerException);
        }

        private static MethodInfo GetStructuredTypeFactoryMethod(Type implementationType, Type itemType, bool withIndex)
        {
            foreach (MethodInfo method in typeof(StructuredType<>).MakeGenericType(implementationType).GetRuntimeMethods())
            {
                if (method.Name != "From")
                    continue;

                IList<ParameterInfo> parameters = method.GetParameters();
                if (parameters.Count != 2)
                    continue;

                if (parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                 && parameters[1].ParameterType.GetGenericTypeDefinition() == (withIndex ? typeof(Action<,,>) : typeof(Action<,>)))
                {
                    return method.MakeGenericMethod(itemType);
                }
            }
            throw new InvalidOperationException("Could not find structured type factory method 'StructuredType<>.From()'");
        }

        private static Type GetItemType(Type enumerableType)
        {
            if (enumerableType.GetInterfaces().All(x => x.GetGenericTypeDefinition() != typeof(IEnumerable<>)))
                throw new InvalidOperationException($"Type does not implement IEnumerable<>: {enumerableType}");

            return enumerableType.GenericTypeArguments[0];
        }
        #endregion

        #region Nested Types
        private sealed class CompilationContext
        {
            public ICollection<ParameterExpression> Parameters { get; }
            public ICollection<ParameterExpression> Variables { get; }
            public ICollection<Expression> Statements { get; }
            public string LastVisitedParameter { get; set; }

            public CompilationContext()
            {
                this.Parameters = new Collection<ParameterExpression>();
                this.Variables = new Collection<ParameterExpression>();
                this.Statements = new Collection<Expression>();
            }
        }

        private sealed class HttpParameterInfo
        {
            public HttpParameterLocation Location { get; }
            public HttpParameterSourceKind SourceKind { get; } 
            public ParameterInfo ContractParameter { get; private set; }    // InputParameterClass
            public Type ParameterType { get; }
            public string InternalParameterName { get; }
            public string ApiParameterName { get; private set; }
            public object Value { get; private set; }
            public bool IsOptional { get; set; }
            public object DefaultValue { get; private set; }
            public Type InputConverter { get; private set; }                // IFormattedInputConverter<TSource, TTarget>
            public IHttpParameterConverter Converter { get; private set; }  // CONVERT(value)
            public HttpParameterSourceInfo Source { get; private set; }     // DBX.LocaleId / databaseAccessorFactory
            public HttpItemsParameterInfo Items { get; private set; }       // udt_columnname = ITEM.SomePropertyName

            private HttpParameterInfo(HttpParameterLocation location, HttpParameterSourceKind sourceKind, Type parameterType, string internalParameterName, bool isOptional)
            {
                this.Location = location;
                this.SourceKind = sourceKind;
                this.ParameterType = parameterType;
                this.InternalParameterName = internalParameterName;
                this.ApiParameterName = internalParameterName;
                this.IsOptional = isOptional;
            }

            public static HttpParameterInfo Query(ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional, object defaultValue)
            {
                HttpParameterInfo parameter = new HttpParameterInfo(HttpParameterLocation.Query, HttpParameterSourceKind.Query, parameterType, parameterName, isOptional)
                {
                    ContractParameter = contractParameter,
                    DefaultValue = defaultValue
                };
                return parameter;
            }
            public static HttpParameterInfo Path(ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional, object defaultValue)
            {
                HttpParameterInfo parameter = new HttpParameterInfo(HttpParameterLocation.Path, HttpParameterSourceKind.Path, parameterType, parameterName, isOptional)
                {
                    ContractParameter = contractParameter,
                    DefaultValue = defaultValue
                };
                return parameter;
            }
            public static HttpParameterInfo SourceInstance(Type parameterType, string parameterName, HttpParameterSourceInfo source)
            {
                HttpParameterInfo parameter = new HttpParameterInfo(HttpParameterLocation.NonUser, HttpParameterSourceKind.SourceInstance, parameterType, parameterName, isOptional: false)
                {
                    Source = source
                };
                source.Parent = parameter;
                return parameter;
            }
            public static HttpParameterInfo SourceProperty(ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional, object defaultValue, HttpParameterSourceInfo source, IHttpParameterConverter converter)
            {
                HttpParameterLocation location = source.Location;
                HttpParameterInfo parameter = new HttpParameterInfo(location, HttpParameterSourceKind.SourceProperty, parameterType, parameterName, isOptional)
                {
                    ContractParameter = contractParameter,
                    DefaultValue = defaultValue,
                    Converter = converter,
                    Source = source
                };
                
                if (IsUri(location))
                    parameter.ApiParameterName = source.PropertyPath;

                if (location == HttpParameterLocation.Header)
                    parameter.IsOptional = true;
                
                source.Parent = parameter;

                return parameter;
            }
            public static HttpParameterInfo SourcePropertyItemsSource(ParameterInfo contractParameter, Type parameterType, string parameterName, HttpParameterSourceInfo source, string udtName, MethodInfo addItemMethod, IEnumerable<HttpParameterInfo> itemSources)
            {
                HttpParameterLocation location = source.Location;
                HttpParameterInfo parameter = new HttpParameterInfo(location, HttpParameterSourceKind.SourceProperty, parameterType, parameterName, isOptional: false)
                {
                    ContractParameter = contractParameter,
                    Source = source,
                    Items = new HttpItemsParameterInfo(udtName, addItemMethod, itemSources)
                };

                if (IsUri(location))
                    parameter.ApiParameterName = source.PropertyPath;

                source.Parent = parameter;

                return parameter;
            }

            public static HttpParameterInfo ConstantValue(ParameterInfo contractParameter, Type parameterType, string parameterName, object value)
            {
                HttpParameterInfo parameter = new HttpParameterInfo(HttpParameterLocation.NonUser, sourceKind: HttpParameterSourceKind.ConstantValue, parameterType, parameterName, isOptional: false)
                {
                    ContractParameter = contractParameter,
                    Value = value
                };
                return parameter;
            }
            public static HttpParameterInfo Body(ParameterInfo contractParameter, Type parameterType, string parameterName, Type inputConverter = null)
            {
                HttpParameterInfo parameter = new HttpParameterInfo(HttpParameterLocation.NonUser, HttpParameterSourceKind.Body, parameterType, parameterName, isOptional: false)
                {
                    ContractParameter = contractParameter,
                    InputConverter = inputConverter
                };
                return parameter;
            }
        }

        private enum HttpParameterSourceKind
        {
            None,
            Query,
            Path,
            SourceInstance,
            SourceProperty,
            ConstantValue,
            Body
        }

        private sealed class HttpParameterSourceInfo : IHttpParameterResolutionContext
        {
            private readonly CompilationContext _compilationContext;
            private readonly IDictionary<string, Expression> _sourceMap;
            private readonly IHttpParameterSourceProvider _sourceProvider;

            public IHttpActionDescriptor Action { get; }
            public Expression RequestParameter { get; }
            public Expression ArgumentsParameter { get; }
            public Expression DependencyResolverParameter { get; }
            public Expression ActionParameter { get; }
            public string SourceName { get; }
            public string PropertyPath { get; }
            public HttpParameterInfo Parent { get; set; }
            public Expression Value { get; private set; }
            public HttpParameterLocation Location => this._sourceProvider?.Location ?? HttpParameterLocation.NonUser;

            public HttpParameterSourceInfo(IHttpActionDescriptor action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, Expression actionParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, string sourceName, IHttpParameterSourceProvider sourceProvider, string propertyPath)
            {
                this._compilationContext = compilationContext;
                this._sourceMap = sourceMap;
                this._sourceProvider = sourceProvider;
                this.Action = action;
                this.RequestParameter = requestParameter;
                this.ArgumentsParameter = argumentsParameter;
                this.DependencyResolverParameter = dependencyResolverParameter;
                this.ActionParameter = actionParameter;
                this.SourceName = sourceName;
                this.PropertyPath = propertyPath;
            }

            public void ResolveUsingInstanceProperty(Type instanceType, Expression instanceValue, bool ensureNullPropagation) => this.ResolveUsingInstanceProperty(instanceType, instanceValue, ensureNullPropagation, this.PropertyPath);
            public void ResolveUsingInstanceProperty(Type instanceType, Expression instanceValue, bool ensureNullPropagation, string propertyPath)
            {
                if (!this._sourceMap.TryGetValue(this.SourceName, out Expression sourceVariableInstance))
                {
                    // Ensure source instance variable
                    ParameterExpression sourceVariable = Expression.Variable(instanceType, $"{this.SourceName.ToLowerInvariant()}Source");
                    Expression sourceAssign = Expression.Assign(sourceVariable, instanceValue);
                    this._compilationContext.Variables.Add(sourceVariable);
                    this._compilationContext.Statements.Add(sourceAssign);
                    this._sourceMap.Add(this.SourceName, sourceVariable);
                    sourceVariableInstance = sourceVariable;
                }

                // Establish property access on instance
                this.Value = sourceVariableInstance;
                if (this.PropertyPath == null)
                    return;

                this.Value = CollectSourcePropertyValue(this.Action, this.RequestParameter, this.ArgumentsParameter, this.DependencyResolverParameter, this.ActionParameter, this._compilationContext, this._sourceMap, this.Parent, this.Value, ensureNullPropagation, propertyPath);
            }

            public void ResolveUsingValue(Expression value) => this.Value = value;

            public void CollectSourceInstance() => this._sourceProvider.Resolve(this);
        }

        private sealed class HttpParameterConversionContext : IHttpParameterConversionContext
        {
            public IHttpActionDescriptor Action { get; }
            public Expression RequestParameter { get; }
            public Expression DependencyResolverParameter { get; }
            public Expression ActionParameter { get; }

            public HttpParameterConversionContext(IHttpActionDescriptor action, Expression requestParameter, Expression dependencyResolverParameter, Expression actionParameter)
            {
                Action = action;
                RequestParameter = requestParameter;
                DependencyResolverParameter = dependencyResolverParameter;
                ActionParameter = actionParameter;
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
            private readonly IHttpActionDescriptor _action;
            private readonly ResolveParameters _compiled;

            public MethodInfo Method => _action.Target;
            public string Source { get; }
            public IDictionary<string, HttpActionParameter> Parameters { get; }

            public HttpParameterResolutionMethod(IHttpActionDescriptor action, string source, ResolveParameters compiled)
            {
                this._action = action;
                this._compiled = compiled;
                this.Source = source;
                this.Parameters = new Dictionary<string, HttpActionParameter>();
            }

            public void AddParameter(string name, Type type, HttpParameterLocation location, bool isOptional) => this.Parameters.Add(name, new HttpActionParameter(name, type, location, isOptional));

            public void PrepareParameters(IHttpRequestDescriptor request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver)
            {
                this._compiled(request, arguments, dependencyResolver, this._action);
            }
        }
        #endregion
    }
}