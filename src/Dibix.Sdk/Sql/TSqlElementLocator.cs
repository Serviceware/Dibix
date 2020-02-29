using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    internal sealed class TSqlElementLocator
    {
        private readonly Lazy<IDictionary<int, ElementLocation>> _elementLocationsAccessor;
        private readonly Lazy<TSqlModel> _modelAccessor;

        public TSqlElementLocator(Lazy<TSqlModel> modelAccessor, TSqlFragment sqlFragment)
        {
            this._modelAccessor = modelAccessor;
            this._elementLocationsAccessor = new Lazy<IDictionary<int, ElementLocation>>(() => AnalyzeSchema(modelAccessor.Value, sqlFragment).Locations.ToDictionary(x => x.Offset));
        }

        public bool TryGetElementLocation(TSqlFragment fragment, out ElementLocation location) => this._elementLocationsAccessor.Value.TryGetValue(fragment.StartOffset, out location);
        
        public bool TryGetModelElement(TSqlFragment fragment, out TSqlObject element)
        {
            if (this._elementLocationsAccessor.Value.TryGetValue(fragment.StartOffset, out ElementLocation location))
            {
                element = location.GetModelElement(this._modelAccessor.Value);
                return element != null;
            }

            element = null;
            return false;
        }

        private static SchemaAnalyzerResult AnalyzeSchema(TSqlModel model, TSqlFragment sqlFragment)
        {
            SchemaAnalyzerResult schemaAnalyzerResult = SchemaAnalyzer.Analyze(model, sqlFragment);
            //if (schemaAnalyzerResult.Errors.Any())
            //    throw new AggregateException("One or more errors occured while validating model schema", schemaAnalyzerResult.Errors.Select(ToException));

            return schemaAnalyzerResult;
        }

        private static Exception ToException(ExtensibilityError error)
        {
            return new InvalidOperationException($"{error.Document}({error.Line},{error.Column}) : {error.Severity} {error.ErrorCode}: {error.Message}", error.Exception);
        }
    }
}