﻿using System;
using System.Linq.Expressions;

namespace Dibix.Http
{
    public class HttpParameterDependencySourceProvider : HttpParameterPropertySourceProvider, IHttpParameterSourceProvider
    {
        private readonly Type _type;

        public HttpParameterDependencySourceProvider(Type type) => this._type = type;

        protected override Type GetInstanceType(IHttpParameterResolutionContext context) => this._type;

        protected override Expression GetInstanceValue(Type instanceType, Expression requestParameter, Expression argumentsParameter, Expression dependencyResolverParameter) => Expression.Call(dependencyResolverParameter, nameof(IParameterDependencyResolver.Resolve), new[] { instanceType });
    }
}