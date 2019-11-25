﻿using System;
using System.Linq.Expressions;

namespace Dibix.Http
{
    public interface IHttpParameterSourceProvider
    {
        Type GetInstanceType(HttpActionDefinition action);
        Expression GetInstanceValue(Type instanceType, ParameterExpression requestParameter, ParameterExpression argumentsParameter, ParameterExpression dependencyProviderParameter);
    }
}