using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            CompilationContext compilationContext = new CompilationContext();
            try
            {
                // (HttpRequestMessage request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver) => 
                ParameterExpression requestParameter = Expression.Parameter(typeof(HttpRequestMessage), "request");
                ParameterExpression argumentsParameter = Expression.Parameter(typeof(IDictionary<string, object>), "arguments");
                ParameterExpression dependencyResolverParameter = Expression.Parameter(typeof(IParameterDependencyResolver), "dependencyResolver");
                compilationContext.Parameters.Add(requestParameter);
                compilationContext.Parameters.Add(argumentsParameter);
                compilationContext.Parameters.Add(dependencyResolverParameter);
                
                IDictionary<string, Expression> sourceMap = new Dictionary<string, Expression>();
                ICollection<HttpParameterInfo> @params = CollectParameters(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameters).ToArray();

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
                // input.id = HttpParameterResolver.ConvertValue<object, int>("id", arguments["id"]);
                // HttpParameterResolver.BindParametersFromBody<SomeValueFromBody, SomeInputParameterClass, SomeInputBinder>(arguments, input, "input");
                // arguments["input"] = input;
                //
                // input.someUdt1 = HttpParameterResolver.ConvertParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments);
                // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt1, SomeUdt1InputConverter>(arguments, "someUdt1");
                // HttpParameterResolver.AddParameterFromBody<SomeValueFromBody, SomeUdt2, SomeUdt2InputConverter>(arguments, "someUdt2");
                // ...
                CollectParameterAssignments(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, @params);

                ResolveParameters compiled = Compile(compilationContext, out string source);
                HttpParameterResolutionMethod method = new HttpParameterResolutionMethod(source, compiled);
                CollectApiParameters(action, method, @params, action.BodyContract);
                return method;
            }
            catch (Exception exception)
            {
                throw CreateException("Http parameter resolver compilation failed", exception, action, compilationContext.LastVisitedParameter);
            }
        }
        #endregion

        #region Private Methods
        private static IEnumerable<HttpParameterInfo> CollectParameters(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, IEnumerable<ParameterInfo> parameters)
        {
            HashSet<string> pathParameters = new HashSet<string>();
            if (!String.IsNullOrEmpty(action.ChildRoute))
                pathParameters.AddRange(HttpParameterUtility.ExtractPathParameters(action.ChildRoute).Select(x => x.Value));

            foreach (ParameterInfo parameter in parameters)
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
                    HttpParameterSourceInfo source = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, sourceName, sourceProvider, propertyPath: null);
                    yield return HttpParameterInfo.SourceInstance(parameter.ParameterType, parameter.Name, source);
                    continue;
                }

                if (parameter.IsDefined(typeof(InputClassAttribute)))
                {
                    foreach (PropertyInfo property in parameter.ParameterType.GetRuntimeProperties())
                    {
                        if (action.ParameterSources.TryGetValue(property.Name, out HttpParameterSource parameterSource))
                        {
                            yield return CollectExplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameterSource, parameter, property.PropertyType, property.Name, isOptional: false, defaultValue: null);
                        }
                        else if (action.BodyBinder != null)
                        {
                            yield return HttpParameterInfo.Body(parameter, property.PropertyType, property.Name);
                        }
                        else
                        {
                            yield return CollectImplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, pathParameters, sourceMap, parameter, property.PropertyType, property.Name, false);
                        }
                    }
                }
                else
                {
                    if (action.ParameterSources.TryGetValue(parameter.Name, out HttpParameterSource parameterSource))
                    {
                        yield return CollectExplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameterSource, contractParameter: null, parameter.ParameterType, parameter.Name, parameter.IsOptional, parameter.DefaultValue);
                    }
                    else if (action.BodyBinder != null)
                    {
                        throw new InvalidOperationException($"Using a binder for the body is only supported if the target parameter is a class and is marked with the {typeof(InputClassAttribute)}");
                    }
                    else
                    {
                        yield return CollectImplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, pathParameters, sourceMap, contractParameter: null, parameter.ParameterType, parameter.Name, parameter.IsOptional, parameter.DefaultValue);
                    }
                }
            }

            compilationContext.LastVisitedParameter = null;
        }

        private static HttpParameterInfo CollectExplicitParameter(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterSource source, ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional, object defaultValue)
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

                        HttpParameterSourceInfo sourceInfo = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, propertySource.SourceName, sourceProvider, propertySource.PropertyPath);
                        return HttpParameterInfo.SourceProperty(contractParameter, parameterType, parameterName, isOptional, defaultValue, sourceInfo, converter);
                    }

                    return CollectItemsParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, contractParameter, parameterType, parameterName, isOptional, defaultValue, propertySource, sourceProvider);

                case HttpParameterBodySource bodySource:
                    Type inputConverter = Type.GetType(bodySource.ConverterName, true);
                    return HttpParameterInfo.Body(contractParameter, parameterType, parameterName, inputConverter);

                case HttpParameterConstantSource constantSource:
                    return HttpParameterInfo.ConstantValue(contractParameter, parameterType, parameterName, constantSource.Value);

                default:
                    throw new InvalidOperationException($"Unsupported parameter source type: '{source.GetType()}'");
            }
        }

        private static HttpParameterInfo CollectImplicitParameter(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, ICollection<string> pathParameters, IDictionary<string, Expression> sourceMap, ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional) => CollectImplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, pathParameters, sourceMap, contractParameter, parameterType, parameterName, isOptional, null);
        private static HttpParameterInfo CollectImplicitParameter(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, ICollection<string> pathParameters, IDictionary<string, Expression> sourceMap, ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional, object defaultValue)
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
                return CollectItemsParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, contractParameter, parameterType, parameterName, isOptional, defaultValue, new HttpParameterPropertySource(BodyParameterSourceProvider.SourceName, sourcePropertyName, null), sourceProvider);

            HttpParameterSourceInfo sourceInfo = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, BodyParameterSourceProvider.SourceName, sourceProvider, propertyPath: sourcePropertyName);
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

        private static HttpParameterInfo CollectItemsParameter(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, ParameterInfo contractParameter, Type parameterType, string parameterName, bool isOptional, object defaultValue, HttpParameterPropertySource propertySource, IHttpParameterSourceProvider sourceProvider)
        {
            string udtName = parameterType.GetCustomAttribute<StructuredTypeAttribute>()?.UdtName;
            MethodInfo addMethod = parameterType.GetMethod("Add");
            if (addMethod == null)
                throw new InvalidOperationException($"Could not find 'Add' method on type: {parameterType}");

            IDictionary<string, Type> parameterMap = addMethod.GetParameters().ToDictionary(x => x.Name, x => x.ParameterType);
            IEnumerable<HttpParameterInfo> itemSources = propertySource.ItemSources.Select(x =>
            {
                if (!parameterMap.TryGetValue(x.Key, out Type itemParameterType))
                    throw new InvalidOperationException($"Target name does not match a UDT column: {udtName ?? parameterType.ToString()}.{x.Key}");

                return CollectExplicitParameter(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, x.Value, null, itemParameterType, x.Key, isOptional, defaultValue);
            });
            HttpParameterSourceInfo source = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, propertySource.SourceName, sourceProvider, propertySource.PropertyPath);
            return HttpParameterInfo.SourcePropertyItemsSource(contractParameter, parameterType, parameterName, source, udtName, addMethod, itemSources);
        }

        private static void CollectParameterSources(IEnumerable<HttpParameterInfo> @params)
        {
            foreach (HttpParameterInfo parameter in @params)
                parameter.Source?.CollectSourceInstance();
        }

        private static void CollectParameterAssignments(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, IEnumerable<HttpParameterInfo> parameters)
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
                            CollectUriParameterAssignment(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameter);
                        }
                        else
                        {
                            // arguments[lcid"] = actionAuthorizationContext.LocaleId;
                            CollectNonUserParameterAssignment(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameter);
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
                            // input.id = HttpParameterResolver.ConvertValue<object, int>("id", arguments["id"]);
                            CollectUriPropertyAssignment(action, requestParameter, dependencyResolverParameter, argumentsParameter, compilationContext, sourceMap, parameter, contractParameterVariable);
                        }
                        else
                        {
                            // input.lcid = actionAuthorizationContext.LocaleId;
                            CollectNonUserPropertyAssignment(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameter, contractParameterVariable);
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

        private static void CollectNonUserParameterAssignment(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter)
        {
            // arguments["lcid"] = actionAuthorizationContext.LocaleId;
            Expression value = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameter);
            CollectParameterAssignment(compilationContext, parameter.InternalParameterName, argumentsParameter, value);
        }

        private static void CollectNonUserPropertyAssignment(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter, Expression contractParameterVariable)
        {
            // input.lcid = actionAuthorizationContext.LocaleId;
            Expression property = Expression.Property(contractParameterVariable, parameter.InternalParameterName);
            Expression value = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameter);
            Expression assign = Expression.Assign(property, value); 
            compilationContext.Statements.Add(assign);
        }

        private static void CollectUriParameterAssignment(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter)
        {
            bool hasDefaultValue = parameter.DefaultValue != DBNull.Value && parameter.DefaultValue != null;

            if (parameter.SourceKind == HttpParameterSourceKind.SourceProperty) // QUERY or PATH source
            {
                // arguments["lcid] = arguments["localeid"];
                Expression value = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameter, generateConverterStatement: !hasDefaultValue, ensureCorrectType: !hasDefaultValue);
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
                Expression value = HttpParameterResolverUtility.BuildArgumentAccessorExpression(argumentsParameter, parameter.InternalParameterName);
                value = CollectConverterStatement(parameter, value);
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
            Expression tryGetValue = Expression.Call(argumentsParameter, nameof(IDictionary<object, object>.TryGetValue), new Type[0], argumentsKey, defaultValue);
            Expression emptyParameterValue = Expression.Equal(defaultValue, Expression.Constant(null));
            Expression condition = Expression.And(tryGetValue, emptyParameterValue);
            Expression property = Expression.Property(argumentsParameter, "Item", argumentsKey);
            Expression value = Expression.Convert(Expression.Constant(parameter.DefaultValue), typeof(object));
            Expression assign = Expression.Assign(property, value);
            Expression @if = Expression.IfThen(condition, assign);
            compilationContext.Variables.Add(defaultValue);
            compilationContext.Statements.Add(@if);
        }

        private static void CollectUriPropertyAssignment(HttpActionDefinition action, Expression requestParameter, Expression dependencyResolverParameter, Expression argumentsParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter, Expression contractParameterVariable)
        {
            // input.id = CONVERT(HttpParameterResolver.ConvertValue<object, int>("id", arguments["id"]));
            Expression property = Expression.Property(contractParameterVariable, parameter.InternalParameterName);
            Expression value = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, parameter);
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
            Expression property = HttpParameterResolverUtility.BuildArgumentAccessorExpression(argumentsParameter, parameterName);
            Expression valueCast = Expression.Convert(value, typeof(object));
            Expression assign = Expression.Assign(property, valueCast);
            compilationContext.Statements.Add(assign);
        }

        private static Expression CollectParameterValue(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, HttpParameterInfo parameter, bool generateConverterStatement = true, bool ensureCorrectType = true)
        {
            Expression value;

            switch (parameter.SourceKind)
            {
                case HttpParameterSourceKind.ConstantValue:
                    value = Expression.Constant(parameter.Value);
                    break;

                case HttpParameterSourceKind.Query:
                case HttpParameterSourceKind.Path:
                    value = HttpParameterResolverUtility.BuildArgumentAccessorExpression(argumentsParameter, parameter.InternalParameterName);
                    break;

                case HttpParameterSourceKind.SourceInstance:
                case HttpParameterSourceKind.SourceProperty:
                    if (sourceMap.TryGetValue(parameter.Source.SourceName, out value))
                    {
                        // Expand property path on an existing source instance variable
                        if (parameter.SourceKind == HttpParameterSourceKind.SourceProperty)
                        {
                            foreach (string propertyName in parameter.Source.PropertyPath.Split('.'))
                            {
                                MemberExpression sourcePropertyExpression = Expression.Property(value, propertyName);
                                value = parameter.Items != null ? CollectItemsParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, parameter, sourcePropertyExpression, sourceMap) : sourcePropertyExpression;
                            }
                        }
                    }
                    else
                        // Use resolved static value from source provider
                        value = parameter.Source.Value;

                    break;

                default:
                    throw new InvalidOperationException($"Value of parameter '{parameter.InternalParameterName}' could not be resolved");
            }

            if (generateConverterStatement) 
                value = CollectConverterStatement(parameter, value);

            // ResolveParameterFromNull
            if (parameter.SourceKind == HttpParameterSourceKind.ConstantValue && parameter.Value == null) 
                return value;

            if (ensureCorrectType)
                value = EnsureCorrectType(parameter.InternalParameterName, value, parameter.ParameterType);

            return value;
        }

        private static Expression CollectItemsParameterValue(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, HttpParameterInfo parameter, MemberExpression sourcePropertyExpression, IDictionary<string, Expression> sourceMap)
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
                if (itemSource.SourceKind == HttpParameterSourceKind.SourceProperty && itemSource.Source.PropertyPath == ItemIndexPropertyName) // ITEM.$INDEX => i
                {
                    HttpParameterSourceInfo source = new HttpParameterSourceInfo(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, ItemSourceName, sourceProvider: null, propertyPath: null);
                    itemParameter = HttpParameterInfo.SourceInstance(itemSource.ParameterType, itemSource.InternalParameterName, source);
                    itemSourceVariable = addItemFuncIndexParameter;
                }
                else
                {
                    itemParameter = itemSource;
                    itemSourceVariable = addItemFuncItemParameter;
                }

                sourceMap[ItemSourceName] = itemSourceVariable;
                addMethodParameterValues[itemSource.InternalParameterName] = CollectParameterValue(action, requestParameter, argumentsParameter, dependencyResolverParameter, compilationContext, sourceMap, itemParameter);
            }

            foreach (KeyValuePair<string, Expression> addMethodParameter in addMethodParameterValues.Where(x => x.Value == null).ToArray())
            {
                PropertyInfo property = itemType.GetProperty(addMethodParameter.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property == null)
                    throw new InvalidOperationException($@"Can not map UDT column: {parameter.Items.UdtName ?? parameter.ParameterType.ToString()}.{addMethodParameter.Key}
Either create a mapping or make sure a property of the same name exists in the source: {parameter.Source.SourceName}.{parameter.Source.PropertyPath}");

                Expression source = Expression.Property(addItemFuncItemParameter, property);
                Type targetType = addMethodParameterTypes[addMethodParameter.Key];
                source = EnsureCorrectType(addMethodParameter.Key, source, targetType);
                addMethodParameterValues[addMethodParameter.Key] = source;
            }

            MethodCallExpression addItemCall = Expression.Call(addItemFuncSetParameter, parameter.Items.AddItemMethod, addMethodParameterValues.Values);
            Expression addItemLambda = Expression.Lambda(addItemCall, addItemFuncSetParameter, addItemFuncItemParameter, addItemFuncIndexParameter);
            Expression value = Expression.Call(null, structuredTypeFactoryMethod, sourcePropertyExpression, addItemLambda);
            return value;
        }

        private static Expression CollectConverterStatement(HttpParameterInfo parameter, Expression value)
        {
            if (parameter.Converter == null)
                return value;

            value = EnsureCorrectType(parameter.InternalParameterName, value, parameter.Converter.ExpectedInputType);
            value = parameter.Converter.ConvertValue(value);
            return value;
        }

        private static Expression EnsureCorrectType(string parameterName, Expression valueExpression, Type targetType)
        {
            if (valueExpression.Type == targetType)
                return valueExpression;

            // Don't generate unnecessary convert calls
            if (targetType == typeof(object))
                return valueExpression;

            Expression convertCall = Expression.Call(typeof(HttpParameterResolver), nameof(ConvertValue), new[] { valueExpression.Type, targetType }, Expression.Constant(parameterName), valueExpression);
            return convertCall;
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

        private static void CollectApiParameters(HttpActionDefinition action, HttpParameterResolutionMethod method, ICollection<HttpParameterInfo> parameters, Type bodyContract)
        {
            if (bodyContract != null)
            {
                // For custom reflection targets we still support handling the body themselves
                // Since the body can only be bound to one target parameter, we skip our $body argument, if necessary
                bool isCustomReflectionTarget = action.Target is ReflectionHttpActionTarget reflectionTarget && reflectionTarget.IsExternal;
                if (parameters.All(x => x.ParameterType != bodyContract) || !isCustomReflectionTarget)
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

        private static TTarget ConvertValue<TSource, TTarget>(string parameterName, TSource value)
        {
            try
            {
                object result = null;
                //if (!Equals(value, null))
                {
                    TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(TTarget));
                    result = typeConverter.CanConvertFrom(typeof(TSource)) ? typeConverter.ConvertFrom(value) : Convert.ChangeType(value, typeof(TTarget));
                }
                return (TTarget)result;
            }
            catch (Exception exception)
            {
                throw CreateException("Parameter mapping failed", exception, null, parameterName);
            }
        }

        private static bool IsUri(HttpParameterLocation location) => location == HttpParameterLocation.Query || location == HttpParameterLocation.Path;

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
            public bool IsOptional { get; }
            public object DefaultValue { get; private set; }
            public Type InputConverter { get; private set; }                // IFormattedInputConverter<TSource, TTarget>
            public IHttpParameterConverter Converter { get; private set; }  // CONVERT(value)
            public HttpParameterSourceInfo Source { get; private set; }     // HLSESSION.LocaleId / databaseAccessorFactory
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
                HttpParameterLocation location = DetermineParameterLocation(source);
                HttpParameterInfo parameter = new HttpParameterInfo(location, HttpParameterSourceKind.SourceProperty, parameterType, parameterName, isOptional)
                {
                    ContractParameter = contractParameter,
                    DefaultValue = defaultValue,
                    Converter = converter,
                    Source = source
                };
                
                if (IsUri(location))
                    parameter.ApiParameterName = source.PropertyPath;
                
                source.Parent = parameter;

                return parameter;
            }
            public static HttpParameterInfo SourcePropertyItemsSource(ParameterInfo contractParameter, Type parameterType, string parameterName, HttpParameterSourceInfo source, string udtName, MethodInfo addItemMethod, IEnumerable<HttpParameterInfo> itemSources)
            {
                HttpParameterLocation location = DetermineParameterLocation(source);
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

            private static HttpParameterLocation DetermineParameterLocation(HttpParameterSourceInfo source)
            {
                switch (source.SourceName)
                {
                    case QueryParameterSourceProvider.SourceName: return HttpParameterLocation.Query;
                    case PathParameterSourceProvider.SourceName: return HttpParameterLocation.Path;
                    default: return HttpParameterLocation.NonUser;
                }
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

            public HttpActionDefinition Action { get; }
            public Expression RequestParameter { get; }
            public Expression ArgumentsParameter { get; }
            public Expression DependencyResolverParameter { get; }
            public string SourceName { get; }
            public string PropertyPath { get; }
            public HttpParameterInfo Parent { get; set; }
            public Expression Value { get; private set; }

            public HttpParameterSourceInfo(HttpActionDefinition action, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter, CompilationContext compilationContext, IDictionary<string, Expression> sourceMap, string sourceName, IHttpParameterSourceProvider sourceProvider, string propertyPath)
            {
                this._compilationContext = compilationContext;
                this._sourceMap = sourceMap;
                this._sourceProvider = sourceProvider;
                this.Action = action;
                this.RequestParameter = requestParameter;
                this.ArgumentsParameter = argumentsParameter;
                this.DependencyResolverParameter = dependencyResolverParameter;
                this.SourceName = sourceName;
                this.PropertyPath = propertyPath;
            }

            public void ResolveUsingInstanceProperty(Type instanceType, Expression instanceValue)
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

                foreach (string propertyName in this.PropertyPath.Split('.'))
                {
                    MemberExpression sourcePropertyExpression = Expression.Property(this.Value, propertyName);
                    this.Value = this.Parent.Items != null ? CollectItemsParameterValue(this.Action, this.RequestParameter, this.ArgumentsParameter, this.DependencyResolverParameter, this._compilationContext, this.Parent, sourcePropertyExpression, this._sourceMap) : sourcePropertyExpression;
                }
            }

            public void ResolveUsingValue(Expression value) => this.Value = value;

            public void CollectSourceInstance() => this._sourceProvider.Resolve(this);
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

            public void AddParameter(string name, Type type, HttpParameterLocation location, bool isOptional) => this.Parameters.Add(name, new HttpActionParameter(name, type, location, isOptional));

            public void PrepareParameters(HttpRequestMessage request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver)
            {
                this._compiled(request, arguments, dependencyResolver);
            }
        }
        #endregion
    }
}