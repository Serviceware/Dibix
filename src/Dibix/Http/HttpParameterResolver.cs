using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dibix.Http
{
    public static class HttpParameterResolver
    {
        #region Fields
        private static readonly ICollection<Type> KnownDependencies = new[] { typeof(IDatabaseAccessorFactory) };
        private static readonly Lazy<PropertyAccessor> DebugViewAccessor = new Lazy<PropertyAccessor>(BuildDebugViewAccessor);
        #endregion

        #region Delegates
        private delegate void ResolveParameters(IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver);
        #endregion

        #region Public Methods
        public static IHttpParameterResolutionMethod Compile(HttpActionDefinition action, ICollection<ParameterInfo> parameters)
        {
            HttpParameterResolverCompilationContext context = new HttpParameterResolverCompilationContext();
            ICollection<HttpParameterInfo> @params = CollectParameters(action, parameters).ToArray();

            // (IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver) => 
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(IDictionary<string, object>), "arguments");
            ParameterExpression dependencyResolverParameter = Expression.Parameter(typeof(IParameterDependencyResolver), "dependencyResolver");
            context.Parameters.Add(argumentsParameter);
            context.Parameters.Add(dependencyResolverParameter);

            // IDatabaseAccesorFactory databaseAccesorFactory = dependencyResolver.Resolve<IDatabaseAccesorFactory>();
            // IActionAuthorizationContext actionAuthorizationContext = dependencyResolver.Resolve<IActionAuthorizationContext>();
            // ...
            IDictionary<string, HttpParameterSourceExpression> sources = CollectParameterSources(action, @params, dependencyResolverParameter, argumentsParameter, context);

            // SomeValueFromBody body = HttpParameterResolver.ReadBody<SomeValueFromBody>(arguments);
            //
            // arguments.Add("databaseAccessorFactory", databaseAccessorFactory);
            // arguments.Add("lcid", actionAuthorizationContext.LocaleId);
            // arguments.Add("id", body.Id);
            //
            // SomeInputParameterClass input = new SomeInputParameterClass();
            // input.lcid = actionAuthorizationContext.LocaleId;
            // input.id = (int)arguments["id"];
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
            CollectExpectedParameters(method.Parameters, @params, action.BodyContract);
            return method;
        }
        #endregion

        #region Private Methods
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Helpline.Server.Application.HttpParameterResolverUtility.CreateException(Dibix.Http.HttpActionDefinition,System.String,System.String)")]
        private static IEnumerable<HttpParameterInfo> CollectParameters(HttpActionDefinition action, IEnumerable<ParameterInfo> parameters)
        {
            foreach (ParameterInfo parameter in parameters)
            {
                // [internal] IDatabaseAccessorFactory databaseAccessorFactory
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
                        if (action.DynamicParameters.TryGetValue(property.Name, out HttpParameterSource propertySource))
                        {
                            // [internal] SomeInputParameterClass.lcid, etc..
                            yield return CollectParameter(action, propertySource, parameter, property.Name, property.PropertyType);
                        }
                        else if (action.BodyBinder != null)
                        {
                            // [body] SomeInputParameterClass.id
                            yield return HttpParameterInfo.Body(parameter, property.PropertyType, property.Name);
                        }
                        else
                        {
                            // [user] SomeInputParameterClass.id
                            yield return HttpParameterInfo.Uri(parameter, property.PropertyType, property.Name);
                        }
                    }
                }
                else
                {
                    if (action.DynamicParameters.TryGetValue(parameter.Name, out HttpParameterSource parameterSource))
                    {
                        // [internal] lcid, etc..
                        yield return CollectParameter(action, parameterSource, null, parameter.Name, parameter.ParameterType);
                    }
                    else if (action.BodyBinder != null)
                    {
                        throw HttpParameterResolverUtility.CreateException(action, $"Using a binder for the body is only supported if the target parameter is a class and is marked with the {typeof(InputClassAttribute)}", parameter.Name);
                    }
                    else
                    {
                        // [user] id
                        yield return HttpParameterInfo.Uri(null, parameter.ParameterType, parameter.Name);
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Helpline.Server.Application.HttpParameterResolver.CreateException(Helpline.Server.Common.Infrastructure.HttpActionDefinition,System.String,System.String)")]
        private static HttpParameterInfo CollectParameter(HttpActionDefinition action, HttpParameterSource source, ParameterInfo contractParameter, string parameterName, Type parameterType)
        {
            switch (source)
            {
                case HttpParameterPropertySource propertySource:
                    if (!HttpParameterSourceProviderRegistry.TryGetProvider(propertySource.SourceName, out IHttpParameterSourceProvider sourceProvider))
                        throw HttpParameterResolverUtility.CreateException(action, $"Unknown source provider '{propertySource.SourceName}' for property '{propertySource.PropertyName}'", parameterName);

                    return HttpParameterInfo.SourceProperty(contractParameter, parameterType, parameterName, sourceProvider, propertySource.SourceName, propertySource.PropertyName);

                case HttpParameterBodySource bodySource:
                    Type inputConverter = Type.GetType(bodySource.ConverterName, true);
                    return HttpParameterInfo.Body(contractParameter, parameterType, parameterName, inputConverter);

                case HttpParameterConstantSource constantSource:
                    return HttpParameterInfo.ConstantValue(contractParameter, parameterType, parameterName, constantSource.Value);

                default:
                    throw HttpParameterResolverUtility.CreateException(action, $"Unsupported parameter source type: '{source.GetType()}'", parameterName);
            }
        }

        private static IDictionary<string, HttpParameterSourceExpression> CollectParameterSources(HttpActionDefinition action, IEnumerable<HttpParameterInfo> @params, ParameterExpression dependencyResolverParameter, ParameterExpression argumentsParameter, HttpParameterResolverCompilationContext context)
        {
            IDictionary<string, HttpParameterSourceExpression> sources = @params.Where(x => x.SourceKind == HttpParameterSourceKind.SourceInstance
                                                                                         || x.SourceKind == HttpParameterSourceKind.SourceProperty)
                                                                                .GroupBy(x => new { x.Source.SourceProvider, x.Source.Name })
                                                                                .Select(x => BuildParameterSource(action, x.Key.Name, x.Key.SourceProvider, dependencyResolverParameter, argumentsParameter))
                                                                                .ToDictionary(x => x.Key, x => x.Value);
            foreach (HttpParameterSourceExpression source in sources.Values)
            {
                context.Variables.Add(source.Variable);
                context.Statements.Add(source.Assignment);
            }

            return sources;
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        private static KeyValuePair<string, HttpParameterSourceExpression> BuildParameterSource(HttpActionDefinition action, string sourceName, IHttpParameterSourceProvider sourceProvider, ParameterExpression dependencyResolverParameter, ParameterExpression argumentsParameter)
        {
            Type instanceType = sourceProvider.GetInstanceType(action);
            ParameterExpression sourceVariable = Expression.Variable(instanceType, sourceName.ToLowerInvariant());
            Expression sourceAssign = Expression.Assign(sourceVariable, sourceProvider.GetInstanceValue(instanceType, argumentsParameter, dependencyResolverParameter));
            return new KeyValuePair<string, HttpParameterSourceExpression>(sourceName, new HttpParameterSourceExpression(sourceVariable, sourceAssign));
        }

        private static void CollectParameterAssignments(HttpActionDefinition action, IEnumerable<HttpParameterInfo> parameters, ParameterExpression argumentsParameter, IDictionary<string, HttpParameterSourceExpression> sources, HttpParameterResolverCompilationContext context)
        {
            foreach (IGrouping<ParameterInfo, HttpParameterInfo> parameterGroup in parameters.GroupBy(x => x.ContractParameter))
            {
                ParameterExpression contractParameterVariable = null;
                if (parameterGroup.Key != null)
                {
                    // SomeInputParameterClass input = new SomeInputParameterClass();
                    contractParameterVariable = CollectInputClass(context, parameterGroup.Key);
                }

                foreach (HttpParameterInfo parameter in parameterGroup)
                {
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
                            CollectNonUserParameterAssignment(context, action, parameter, argumentsParameter, sources);
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
                            CollectNonUserPropertyAssignment(context, action, parameter, contractParameterVariable, sources);
                        }
                        else
                        {
                            // input.id = (int)arguments["id"];
                            CollectUserPropertyAssignment(context, parameter, contractParameterVariable, argumentsParameter);
                        }
                    }
                }

                if (contractParameterVariable != null)
                {
                    if (action.BodyBinder != null)
                    {
                        // HttpParameterResolver.BindParametersFromBody<SomeValueFromBody, SomeInputParameterClass, SomeInputBinder>(arguments, input);
                        CollectBodyParameterBindingAssignment(context, action.SafeGetBodyContract(), action.BodyBinder, parameterGroup.Key, contractParameterVariable, argumentsParameter);
                    }

                    // arguments.Add("input", input);
                    CollectParameterAssignment(context, contractParameterVariable.Name, argumentsParameter, contractParameterVariable);
                }
            }
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

        private static void CollectNonUserParameterAssignment(HttpParameterResolverCompilationContext context, HttpActionDefinition action, HttpParameterInfo parameter, Expression target, IDictionary<string, HttpParameterSourceExpression> sources)
        {
            // arguments.Add("lcid", actionAuthorizationContext.LocaleId);
            Expression value = CollectParameterValue(action, parameter, sources);
            CollectParameterAssignment(context, parameter.ParameterName, target, value);
        }

        private static void CollectNonUserPropertyAssignment(HttpParameterResolverCompilationContext context, HttpActionDefinition action, HttpParameterInfo parameter, Expression target, IDictionary<string, HttpParameterSourceExpression> sources)
        {
            // input.lcid = actionAuthorizationContext.LocaleId;
            Expression property = Expression.Property(target, parameter.ParameterName);
            Expression value = CollectParameterValue(action, parameter, sources);
            Expression assign = Expression.Assign(property, value);
            context.Statements.Add(assign);
        }

        private static void CollectUserPropertyAssignment(HttpParameterResolverCompilationContext context, HttpParameterInfo parameter, Expression target, Expression argumentsParameter)
        {
            // input.id = (int)arguments["id"];
            Expression argumentsKey = Expression.Constant(parameter.ParameterName);
            Expression property = Expression.Property(target, parameter.ParameterName);
            Expression value = Expression.Property(argumentsParameter, "Item", argumentsKey);
            Expression valueCast = Expression.Convert(value, parameter.ParameterType);
            Expression assign = Expression.Assign(property, valueCast);
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

        private static Expression CollectParameterValue(HttpActionDefinition action, HttpParameterInfo parameter, IDictionary<string, HttpParameterSourceExpression> sources)
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
                        value = Expression.Property(value, parameter.Source.PropertyName);

                    break;

                default:
                    throw HttpParameterResolverUtility.CreateException(action, $"Value of parameter '{parameter.ParameterName}' could not be resolved");
            }

            if (value.Type != parameter.ParameterType)
                value = Expression.Convert(value, parameter.ParameterType);

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

        private static void CollectExpectedParameters(IDictionary<string, Type> target, IEnumerable<HttpParameterInfo> parameters, Type bodyContract)
        {
            if (bodyContract != null)
                target.Add(HttpParameterResolverUtility.BodyKey, bodyContract);

            foreach (HttpParameterInfo parameter in parameters.Where(x => x.IsUserParameter))
            {
                target.Add(parameter.ParameterName, parameter.ParameterType);
            }
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
        #endregion

        #region Nested Types
        private sealed class HttpParameterResolverCompilationContext
        {
            public ICollection<ParameterExpression> Parameters { get; }
            public ICollection<ParameterExpression> Variables { get; }
            public ICollection<Expression> Statements { get; }

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

        private sealed class HttpParameterSourceInfo
        {
            public IHttpParameterSourceProvider SourceProvider { get; }
            public string Name { get; }
            public string PropertyName { get; }

            public HttpParameterSourceInfo(IHttpParameterSourceProvider sourceProvider, string name, string propertyName = null)
            {
                this.SourceProvider = sourceProvider;
                this.Name = name;
                this.PropertyName = propertyName;
            }
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

        private sealed class HttpParameterInfo
        {
            public bool IsUserParameter { get; }               // Query parameter or body
            public HttpParameterSourceKind SourceKind { get; } 
            public ParameterInfo ContractParameter { get; }    // InputParameterClass
            public Type ParameterType { get; }                 
            public string ParameterName { get; }               
            public object Value { get; }                       
            public Type InputConverter { get; }                // IFormattedInputConverter<TSource, TTarget>
            public HttpParameterSourceInfo Source { get; }     // HLSESSION.LocaleId / databaseAccessorFactory

            private HttpParameterInfo(bool isUserParameter, HttpParameterSourceKind sourceKind, ParameterInfo contractParameter, Type parameterType, string parameterName, object value, Type inputConverter, HttpParameterSourceInfo source)
            {
                this.IsUserParameter = isUserParameter;
                this.SourceKind = sourceKind;
                this.ContractParameter = contractParameter;
                this.ParameterType = parameterType;
                this.ParameterName = parameterName;
                this.Value = value;
                this.InputConverter = inputConverter;
                this.Source = source;
            }

            public static HttpParameterInfo Uri(ParameterInfo contractParameter, Type parameterType, string parameterName) => new HttpParameterInfo(true, HttpParameterSourceKind.Uri, contractParameter, parameterType, parameterName, null, null, null);
            public static HttpParameterInfo SourceInstance(Type parameterType, string parameterName, IHttpParameterSourceProvider sourceProvider, string sourceName) => new HttpParameterInfo(false, HttpParameterSourceKind.SourceInstance, null, parameterType, parameterName, null, null, new HttpParameterSourceInfo(sourceProvider, sourceName));
            public static HttpParameterInfo SourceProperty(ParameterInfo contractParameter, Type parameterType, string parameterName, IHttpParameterSourceProvider sourceProvider, string sourceName, string sourcePropertyName) => new HttpParameterInfo(false, HttpParameterSourceKind.SourceProperty, contractParameter, parameterType, parameterName, null, null, new HttpParameterSourceInfo(sourceProvider, sourceName, sourcePropertyName));
            public static HttpParameterInfo ConstantValue(ParameterInfo contractParameter, Type parameterType, string parameterName, object value) => new HttpParameterInfo(false, HttpParameterSourceKind.ConstantValue, contractParameter, parameterType, parameterName, value, null, null);
            public static HttpParameterInfo Body(ParameterInfo contractParameter, Type parameterType, string parameterName, Type inputConverter = null) => new HttpParameterInfo(false, HttpParameterSourceKind.Body, contractParameter, parameterType, parameterName, null, inputConverter, null);
        }

        private sealed class HttpParameterResolutionMethod : IHttpParameterResolutionMethod
        {
            private readonly ResolveParameters _compiled;

            public string Source { get; }
            public IDictionary<string, Type> Parameters { get; }

            public HttpParameterResolutionMethod(string source, ResolveParameters compiled)
            {
                this._compiled = compiled;
                this.Source = source;
                this.Parameters = new Dictionary<string, Type>();
            }

            public void PrepareParameters(IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver)
            {
                this._compiled(arguments, dependencyResolver);
            }
        }
        #endregion
    }
}