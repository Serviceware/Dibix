using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    public abstract class HttpApiDescriptor
    {
        #region Fields
        private readonly Lazy<string> _areaNameAccessor;
        #endregion

        #region Properties
        public string AreaName => _areaNameAccessor.Value;
        public ICollection<HttpControllerDefinition> Controllers { get; }
        #endregion

        #region Constructor
        protected HttpApiDescriptor()
        {
            Controllers = new Collection<HttpControllerDefinition>();
            _areaNameAccessor = new Lazy<string>(ResolveAreaName);
        }
        #endregion

        #region Abstract Methods
        public abstract void Configure(IHttpApiDiscoveryContext context);
        #endregion

        #region Protected Methods
        protected virtual string ResolveAreaName(Assembly assembly)
        {
            AreaRegistrationAttribute attribute = assembly.GetCustomAttribute<AreaRegistrationAttribute>();
            if (attribute == null)
                throw new InvalidOperationException($"Assembly {assembly.GetName().Name} is not marked with {typeof(AreaRegistrationAttribute)}");

            if (String.IsNullOrEmpty(attribute.AreaName))
                throw new InvalidOperationException($@"Area name cannot be empty
{assembly.GetName().Name} -> {GetType()}");

            return attribute.AreaName;
        }

        protected void RegisterController(string controllerName, Action<IHttpControllerDefinitionBuilder> setupAction)
        {
            Guard.IsNotNullOrEmpty(controllerName, nameof(controllerName));
            HttpControllerDefinitionBuilder builder = new HttpControllerDefinitionBuilder(AreaName, controllerName);
            setupAction(builder);
            HttpControllerDefinition controller = builder.Build();
            Controllers.Add(controller);
        }
        #endregion

        #region Private Methods
        private string ResolveAreaName()
        {
            Assembly assembly = GetType().Assembly;
            return ResolveAreaName(assembly);
        }
        #endregion

        #region Nested Types
        private sealed class HttpControllerDefinitionBuilder : IHttpControllerDefinitionBuilder
        {
            private readonly string _areaName;
            private readonly string _controllerName;
            private readonly IList<HttpActionDefinitionBuilder> _actions;
            private readonly IList<string> _controllerImports;

            internal HttpControllerDefinitionBuilder(string areaName, string controllerName)
            {
                _areaName = areaName;
                _controllerName = controllerName;
                _actions = new Collection<HttpActionDefinitionBuilder>();
                _controllerImports = new Collection<string>();
            }

            public void AddAction(IHttpActionTarget target, Action<IHttpActionDefinitionBuilder> setupAction)
            {
                Guard.IsNotNull(setupAction, nameof(setupAction));
                HttpActionDefinitionBuilder builder = new HttpActionDefinitionBuilder(_areaName, _controllerName, target);
                setupAction(builder);
                _actions.Add(builder);
            }

            public void Import(string fullControllerTypeName) => _controllerImports.Add(fullControllerTypeName);

            public HttpControllerDefinition Build()
            {
                IList<HttpActionDefinition> actions = _actions.Select(x => x.Build()).ToArray();
                HttpControllerDefinition controller = new HttpControllerDefinition(_areaName, _controllerName, actions, _controllerImports);
                foreach (HttpActionDefinition action in actions)
                {
                    action.Controller = controller;
                }
                return controller;
            }
        }

        private sealed class HttpActionDefinitionBuilder : HttpActionBuilderBase, IHttpActionDefinitionBuilder, IHttpActionBuilderBase, IHttpParameterSourceSelector, IHttpActionDescriptor
        {
            private readonly string _areaName;
            private readonly string _controllerName;
            private HttpAuthorizationBuilder _authorization;
            private Uri _uri;

            public MethodInfo Target { get; }
            public HttpApiMethod Method { get; set; }
            public Uri Uri => _uri ??= new Uri(RouteBuilder.BuildRoute(_areaName, _controllerName, ChildRoute), UriKind.Relative);
            public string ChildRoute { get; set; }
            public Type BodyContract { get; set; }
            public Type BodyBinder { get; set; }
            public bool IsAnonymous { get; set; }
            public HttpFileResponseDefinition FileResponse { get; set; }
            public string Description { get; set; }
            public IDictionary<int, HttpErrorResponse> StatusCodeDetectionResponses { get; }
            public Delegate Delegate { get; set; }

            public HttpActionDefinitionBuilder(string areaName, string controllerName, IHttpActionTarget target)
            {
                _areaName = areaName;
                _controllerName = controllerName;
                Target = target.Build();
                StatusCodeDetectionResponses = new Dictionary<int, HttpErrorResponse>(HttpStatusCodeDetectionMap.Defaults);
            }

            public void DisableStatusCodeDetection(int statusCode) => StatusCodeDetectionResponses.Remove(statusCode);
            
            public void SetStatusCodeDetectionResponse(int statusCode, int errorCode, string errorMessage) => StatusCodeDetectionResponses[statusCode] = new HttpErrorResponse(statusCode, errorCode, errorMessage);

            public void WithAuthorization(IHttpActionTarget target, Action<IHttpAuthorizationBuilder> setupAction)
            {
                HttpAuthorizationBuilder builder = new HttpAuthorizationBuilder(this, target);
                Guard.IsNotNull(setupAction, nameof(setupAction));
                setupAction(builder);
                _authorization = builder;
            }

            public void RegisterDelegate(Delegate @delegate) => Delegate = @delegate;

            bool IHttpActionDescriptor.TryGetParameter(string parameterName, out HttpParameterSource value) => ParameterSources.TryGetValue(parameterName, out value);

            public HttpActionDefinition Build()
            {
                IHttpActionExecutionMethod executor = HttpActionExecutorResolver.Compile(this);
                IHttpParameterResolutionMethod parameterResolver = HttpParameterResolver.Compile(this);
                HttpActionDefinition action = new HttpActionDefinition
                {
                    Uri = Uri,
                    Executor = executor,
                    ParameterResolver = parameterResolver,
                    Method = Method,
                    ChildRoute = ChildRoute,
                    Body = BodyContract != null ? new HttpRequestBody(BodyContract, BodyBinder) : null,
                    IsAnonymous = IsAnonymous,
                    FileResponse = FileResponse,
                    Description = Description,
                    Authorization = _authorization?.Build(),
                    Delegate = Delegate
                };
                action.StatusCodeDetectionResponses.AddRange(StatusCodeDetectionResponses);
                return action;
            }
        }

        private sealed class HttpAuthorizationBuilder : HttpActionBuilderBase, IHttpAuthorizationBuilder, IHttpActionDescriptor, IHttpActionBuilderBase, IHttpParameterSourceSelector
        {
            private readonly IHttpActionDescriptor _parent;

            public MethodInfo Target { get; }
            public HttpApiMethod Method => _parent.Method;
            public Uri Uri => _parent.Uri;
            public string ChildRoute => _parent.ChildRoute;
            public Type BodyContract => _parent.BodyContract;
            public Type BodyBinder => _parent.BodyBinder;

            public HttpAuthorizationBuilder(IHttpActionDescriptor parent, IHttpActionTarget target)
            {
                _parent = parent;
                Target = target.Build();
            }

            public HttpAuthorizationDefinition Build()
            {
                IHttpActionExecutionMethod executor = HttpActionExecutorResolver.Compile(this);
                IHttpParameterResolutionMethod parameterResolver = HttpParameterResolver.Compile(this);
                HttpAuthorizationDefinition authorization = new HttpAuthorizationDefinition
                {
                    Executor = executor,
                    ParameterResolver = parameterResolver,
                };
                return authorization;
            }

            bool IHttpActionDescriptor.TryGetParameter(string parameterName, out HttpParameterSource value) => ParameterSources.TryGetValue(parameterName, out value);
        }

        private abstract class HttpActionBuilderBase : HttpParameterSourceSelector, IHttpActionBuilderBase, IHttpParameterSourceSelector
        {
            public void ResolveParameterFromBody(string targetParameterName, string bodyConverterName)
            {
                base.ResolveParameter(targetParameterName, new HttpParameterBodySource(bodyConverterName));
            }

            public void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyName, Action<IHttpParameterSourceSelector> itemSources)
            {
                HttpParameterPropertySource source = base.ResolveParameterFromSourceCore(targetParameterName, sourceName, sourcePropertyName, null);
                if (itemSources == null)
                    return;

                HttpParameterSourceSelector sourceSelector = new HttpParameterSourceSelector();
                itemSources.Invoke(sourceSelector);
                source.ItemSources.AddRange(sourceSelector.ParameterSources);
            }
        }

        private class HttpParameterSourceSelector : IHttpParameterSourceSelector
        {
            internal IDictionary<string, HttpParameterSource> ParameterSources { get; }

            public HttpParameterSourceSelector()
            {
                ParameterSources = new Dictionary<string, HttpParameterSource>(StringComparer.OrdinalIgnoreCase);
            }

            public void ResolveParameterFromConstant(string targetParameterName, bool value)
            {
                ResolveParameter(targetParameterName, new HttpParameterConstantSource(typeof(bool), value));
            }
            public void ResolveParameterFromConstant(string targetParameterName, int value)
            {
                ResolveParameter(targetParameterName, new HttpParameterConstantSource(typeof(int), value));
            }
            public void ResolveParameterFromConstant(string targetParameterName, string value)
            {
                ResolveParameter(targetParameterName, new HttpParameterConstantSource(typeof(string), value));
            }
            public void ResolveParameterFromConstant<T>(string targetParameterName, T value)
            {
                ResolveParameter(targetParameterName, new HttpParameterConstantSource(typeof(T), value));
            }
            public void ResolveParameterFromNull<T>(string targetParameterName)
            {
                ResolveParameter(targetParameterName, new HttpParameterConstantSource(typeof(T), null));
            }
            public void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyPath)
            {
                ResolveParameterFromSourceCore(targetParameterName, sourceName, sourcePropertyPath, null);
            }
            public void ResolveParameterFromSource(string targetParameterName, string sourceName, string sourcePropertyPath, string converterName)
            {
                ResolveParameterFromSourceCore(targetParameterName, sourceName, sourcePropertyPath, converterName);
            }

            protected HttpParameterPropertySource ResolveParameterFromSourceCore(string targetParameterName, string sourceName, string sourcePropertyPath, string converterName)
            {
                HttpParameterPropertySource source = new HttpParameterPropertySource(sourceName, sourcePropertyPath, converterName);
                ResolveParameter(targetParameterName, source);
                return source;
            }

            protected void ResolveParameter(string targetParameterName, HttpParameterSource source)
            {
                ParameterSources.Add(targetParameterName, source);
            }
        }
        #endregion
    }
}