using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Http.Server
{
    public abstract class HttpApiDescriptor
    {
        #region Properties
        public ICollection<HttpControllerDefinition> Controllers { get; } = new List<HttpControllerDefinition>();
        public EndpointMetadata Metadata { get; }
        public Action<string, IHttpActionDefinitionBuilder> ActionConfiguredHandler { get; set; }
        #endregion

        #region Constructor
        protected HttpApiDescriptor()
        {
            Metadata = new EndpointMetadata(GetType());
        }
        #endregion

        #region Abstract Methods
        public abstract void Configure(IHttpApiDiscoveryContext context);
        #endregion

        #region Protected Methods
        protected void RegisterController(string controllerName, Action<IHttpControllerDefinitionBuilder> setupAction)
        {
            Guard.IsNotNullOrEmpty(controllerName, nameof(controllerName));
            HttpControllerDefinitionBuilder builder = new HttpControllerDefinitionBuilder(Metadata, controllerName, ActionConfiguredHandler);
            setupAction(builder);
            HttpControllerDefinition controller = builder.Build();
            Controllers.Add(controller);
        }
        #endregion

        #region Nested Types
        private sealed class HttpControllerDefinitionBuilder : IHttpControllerDefinitionBuilder
        {
            private readonly EndpointMetadata _endpointMetadata;
            private readonly string _controllerName;
            private readonly Action<string, IHttpActionDefinitionBuilder> _actionConfiguredHandler;
            private readonly IList<HttpActionDefinitionBuilder> _actions;

            internal HttpControllerDefinitionBuilder(EndpointMetadata endpointMetadata, string controllerName, Action<string, IHttpActionDefinitionBuilder> actionConfiguredHandler)
            {
                _endpointMetadata = endpointMetadata;
                _controllerName = controllerName;
                _actionConfiguredHandler = actionConfiguredHandler;
                _actions = new List<HttpActionDefinitionBuilder>();
            }

            public void AddAction(IHttpActionTarget target, Action<IHttpActionDefinitionBuilder> setupAction)
            {
                Guard.IsNotNull(setupAction, nameof(setupAction));
                HttpActionDefinitionBuilder builder = new HttpActionDefinitionBuilder(_endpointMetadata, _controllerName, target);
                setupAction(builder);
                _actionConfiguredHandler?.Invoke(_controllerName, builder);
                _actions.Add(builder);
            }

            public HttpControllerDefinition Build()
            {
                IEnumerable<HttpActionDefinition> actions = _actions.Select(x => x.Build());
                HttpControllerDefinition controller = new HttpControllerDefinition(_controllerName, actions);
                return controller;
            }
        }

        private sealed class HttpActionDefinitionBuilder : HttpActionBuilderBase, IHttpActionDefinitionBuilder, IHttpActionDefinitionBuilderInternal, IHttpActionBuilderBase, IHttpParameterSourceSelector, IHttpActionDescriptor, IHttpActionMetadata
        {
            private readonly string _controllerName;
            private readonly ICollection<HttpAuthorizationBuilder> _authorization;
            private readonly Dictionary<int, HttpErrorResponse> _statusCodeDetectionResponses;
            private readonly Dictionary<string, string> _parameterDescriptions;
            private Delegate _delegate;

            public EndpointMetadata Metadata { get; }
            public MethodInfo Target { get; }
            public string ActionName { get; set; }
            public string RelativeNamespace { get; set; }
            public HttpApiMethod Method { get; set; }
            public Uri Uri { get; private set; }
            public string ChildRoute { get; set; }
            public Type BodyContract { get; set; }
            public Type BodyBinder { get; set; }
            public string Description { get; set; }
            public ModelContextProtocolType ModelContextProtocolType { get; set; }
            public ICollection<string> SecuritySchemes { get; }
            public HttpFileResponseDefinition FileResponse { get; set; }

            public HttpActionDefinitionBuilder(EndpointMetadata endpointMetadata, string controllerName, IHttpActionTarget target) : base(target.IsExternal)
            {
                _controllerName = controllerName;
                _authorization = new List<HttpAuthorizationBuilder>();
                Metadata = endpointMetadata;
                Target = target.Build();
                SecuritySchemes = new List<string>();
                _statusCodeDetectionResponses = new Dictionary<int, HttpErrorResponse>(HttpStatusCodeDetection.HttpStatusCodeMap);
                _parameterDescriptions = new Dictionary<string, string>();
            }

            public void DisableStatusCodeDetection(int statusCode) => _statusCodeDetectionResponses.Remove(statusCode);

            public void SetStatusCodeDetectionResponse(int statusCode, int errorCode, string errorMessage) => _statusCodeDetectionResponses[statusCode] = new HttpErrorResponse(statusCode, errorCode, errorMessage);

            public void AddAuthorizationBehavior(IHttpActionTarget target, Action<IHttpAuthorizationBuilder> setupAction)
            {
                HttpAuthorizationBuilder builder = new HttpAuthorizationBuilder(this, target);
                Guard.IsNotNull(setupAction, nameof(setupAction));
                setupAction(builder);
                _authorization.Add(builder);
            }

            public void RegisterDelegate(Delegate @delegate) => _delegate = @delegate;

            bool IHttpActionDescriptor.TryGetParameter(string parameterName, out HttpParameterSource value) => ParameterSources.TryGetValue(parameterName, out value);

            void IHttpActionDefinitionBuilderInternal.AddParameterDescription(string parameterName, string description)
            {
                _parameterDescriptions.Add(parameterName, description);
            }

            public HttpActionDefinition Build()
            {
                // Used by HttpActionExecutorResolver and HttpParameterResolver for error logging
                Uri = new Uri(RouteBuilder.BuildRoute(Metadata.AreaName, _controllerName, ChildRoute), UriKind.Relative);

                IHttpActionExecutionMethod executor = HttpActionExecutorResolver.Compile(this);
                IHttpParameterResolutionMethod parameterResolver = HttpParameterResolver.Compile(this);
                HttpRequestBody body = BodyContract != null ? new HttpRequestBody(BodyContract, BodyBinder) : null;
                HttpActionDefinition action = new HttpActionDefinition
                (
                    Metadata,
                    ActionName,
                    RelativeNamespace,
                    Uri,
                    executor,
                    parameterResolver,
                    Method,
                    ChildRoute,
                    body,
                    FileResponse,
                    Description,
                    ModelContextProtocolType,
                    _delegate,
                    _authorization.Select(x => x.Build()),
                    SecuritySchemes,
                    RequiredClaims,
                    _statusCodeDetectionResponses,
                    _parameterDescriptions
                );
                return action;
            }
        }

        private sealed class HttpAuthorizationBuilder : HttpActionBuilderBase, IHttpAuthorizationBuilder, IHttpActionDescriptor, IHttpActionBuilderBase, IHttpParameterSourceSelector
        {
            private readonly HttpActionDefinitionBuilder _parent;

            public EndpointMetadata Metadata => _parent.Metadata;
            public MethodInfo Target { get; }
            public string ActionName => _parent.ActionName;
            public string RelativeNamespace => _parent.RelativeNamespace;
            public HttpApiMethod Method => _parent.Method;
            public Uri Uri => _parent.Uri;
            public string ChildRoute => _parent.ChildRoute;
            public Type BodyContract => _parent.BodyContract;
            public Type BodyBinder => _parent.BodyBinder;
            protected internal override ICollection<string> RequiredClaims => _parent.RequiredClaims;

            public HttpAuthorizationBuilder(HttpActionDefinitionBuilder parent, IHttpActionTarget target) : base(target.IsExternal)
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
            private readonly bool _isExternalReflectionTarget;

            protected internal virtual ICollection<string> RequiredClaims { get; } = new HashSet<string>();

            protected HttpActionBuilderBase(bool isExternalReflectionTarget)
            {
                _isExternalReflectionTarget = isExternalReflectionTarget;
            }

            public void ResolveParameterFromBody(string targetParameterName, string bodyConverterName)
            {
                base.ResolveParameter(targetParameterName, new HttpParameterBodySource(bodyConverterName));
            }

            public void ResolveParameterFromClaim(string targetParameterName, string claimType)
            {
                ResolveParameter(targetParameterName, new HttpParameterClaimSource(claimType));
                RequiredClaims.Add(claimType);
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

            public void AppendRequiredClaim(string claimType)
            {
                if (!_isExternalReflectionTarget)
                {
                    if (!RequiredClaims.Contains(claimType))
                        throw new InvalidOperationException($"Required claim '{claimType}' not registered");

                    return;
                }
                RegisterRequiredClaim(claimType);
            }

            public void RegisterRequiredClaim(string claimType)
            {
                RequiredClaims.Add(claimType);
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