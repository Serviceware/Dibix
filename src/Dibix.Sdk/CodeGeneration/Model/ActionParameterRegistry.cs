using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterRegistry
    {
        private readonly ActionDefinition _actionDefinition;
        private readonly IDictionary<string, int> _pathSegmentIndexMap;
        private int _previousPathSegmentIndex;
        private int _previousPathParameterIndex = -1;

        public ActionParameterRegistry(ActionDefinition actionDefinition, IDictionary<string, PathParameter> pathParameters)
        {
            this._actionDefinition = actionDefinition;
            this._pathSegmentIndexMap = pathParameters.OrderBy(x => x.Value.Index)
                                                      .Select((x, i) => new
                                                      {
                                                          Index = i,
                                                          Key = x.Key
                                                      })
                                                      .ToDictionary(x => x.Key, x => x.Index);
        }

        public void Add(ActionParameter actionParameter)
        {
            int index = this._previousPathParameterIndex + 1;
            if (actionParameter.Location == ActionParameterLocation.Path)
            {
                // Restore original path parameter order from route template
                int currentPathSegmentIndex = this._pathSegmentIndexMap[actionParameter.ApiParameterName];
                bool insertBefore = this._previousPathSegmentIndex > currentPathSegmentIndex;
                this._previousPathSegmentIndex = currentPathSegmentIndex;
                if (insertBefore)
                {
                    index = this._previousPathParameterIndex;
                    this._previousPathParameterIndex = index;
                }
                this._previousPathParameterIndex++;

            }
            else
                index = this._actionDefinition.Parameters.Count;

            this._actionDefinition.Parameters.Insert(index, actionParameter);
        }
    }
}