using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Http.Server
{
    public static class ProblemDetailsExtensions
    {
        public static IProblemDetailsConfigurationBuilder AddProblemDetailsWithMapping(this IServiceCollection services)
        {
            ProblemDetailsConfigurationBuilder builder = new ProblemDetailsConfigurationBuilder();
            services.AddProblemDetails(builder.Configure);
            return builder;
        }

        private sealed class ProblemDetailsConfigurationBuilder : IProblemDetailsConfigurationBuilder
        {
            private readonly ICollection<Func<ProblemDetailsContext, bool>> _mappers = new List<Func<ProblemDetailsContext, bool>>();

            public void Configure(ProblemDetailsOptions options)
            {
                if (!_mappers.Any())
                    return;

                options.CustomizeProblemDetails = CustomizeProblemDetails;
            }

            public IProblemDetailsConfigurationBuilder Map<TException>(Action<ProblemDetails, TException> handler) => Map(filter: null, handler);
            public IProblemDetailsConfigurationBuilder Map<TException>(Func<TException, bool> filter, Action<ProblemDetails, TException> handler)
            {
                bool HandleException(ProblemDetailsContext context)
                {
                    if (context.Exception is not TException exception)
                        return false;

                    if (filter != null && !filter(exception))
                        return false;

                    handler(context.ProblemDetails, exception);
                    return true;

                }

                _mappers.Add(HandleException);
                return this;
            }

            private void CustomizeProblemDetails(ProblemDetailsContext context)
            {
                foreach (Func<ProblemDetailsContext, bool> mapper in _mappers)
                {
                    if (mapper(context))
                        return;
                }
            }
        }
    }

    public interface IProblemDetailsConfigurationBuilder
    {
        IProblemDetailsConfigurationBuilder Map<TException>(Action<ProblemDetails, TException> exception);
        IProblemDetailsConfigurationBuilder Map<TException>(Func<TException, bool> filter, Action<ProblemDetails, TException> exception);
    }
}