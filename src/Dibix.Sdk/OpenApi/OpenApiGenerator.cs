using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Http;
using Dibix.Sdk.CodeGeneration;
using Microsoft.OpenApi.Models;

namespace Dibix.Sdk.OpenApi
{
    internal static class OpenApiGenerator
    {
        public static OpenApiDocument Generate
        (
            string title
          , string areaName
          , IEnumerable<ControllerDefinition> controllers
          , IEnumerable<ContractDefinition> contracts
        )
        {
            OpenApiDocument document = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = title,
                    Version = "1.0.0"
                }
            };
            foreach (ControllerDefinition controller in controllers)
            {
                foreach (IGrouping<string, ActionDefinition> path in controller.Actions.GroupBy(x => $"/{RouteBuilder.BuildRoute(areaName, controller.Name, x.ChildRoute)}"))
                {
                    OpenApiPathItem value = new OpenApiPathItem();
                    
                    foreach (ActionDefinition action in path)
                    {
                        OperationType operationType = (OperationType)Enum.Parse(typeof(OperationType), action.Method.ToString());
                        string operationName = action.Target.Target.Split(',').First().Split('.').Last();

                        OpenApiOperation operation = new OpenApiOperation();
                        operation.Tags.Add(new OpenApiTag { Name = $"{areaName}/{controller.Name}" });
                        operation.Summary = "Undocumented action";
                        operation.OperationId = $"{areaName}_{controller}_{operationName}";
                        if (action.BodyContract != null)
                        {
                            //operation.RequestBody = new OpenApiRequestBody
                            //{
                            //    Required = true,
                            //    Content =
                            //    {
                            //        {
                            //            HttpParameterResolverUtility.BodyKey, new OpenApiMediaType
                            //            {
                            //                Schema = new OpenApiSchema
                            //                {
                                                
                            //                }
                            //            }
                            //        }
                            //    }
                            //};
                        }
                        //operation.Responses.Add(((int)HttpStatusCode.OK).ToString(), new OpenApiResponse
                        //{
                            
                        //});

                        value.AddOperation(operationType, operation);
                    }

                    if (document.Paths == null)
                        document.Paths = new OpenApiPaths();

                    document.Paths.Add(path.Key, value);
                }
            }
            return document;
        }
    }
}