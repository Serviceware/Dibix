using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterRegistry
    {
        private readonly ActionTargetDefinition _actionTargetDefinition;
        private readonly IDictionary<string, int> _pathSegmentIndexMap;
        private int _previousPathSegmentIndex;
        private int _previousPathParameterIndex = -1;

        public ActionParameterRegistry(ActionTargetDefinition actionTargetDefinition, IDictionary<string, PathParameter> pathParameters)
        {
            _actionTargetDefinition = actionTargetDefinition;
            _pathSegmentIndexMap = pathParameters.OrderBy(x => x.Value.Index)
                                                      .Select((x, i) => new
                                                      {
                                                          Index = i,
                                                          Key = x.Key
                                                      })
                                                      .ToDictionary(x => x.Key, x => x.Index, StringComparer.OrdinalIgnoreCase);
        }

        public void Add(ActionParameter actionParameter)
        {
            int index = _previousPathParameterIndex + 1;
            if (actionParameter.Location == ActionParameterLocation.Path)
            {
                // Restore original path parameter order from route template
                if (!_pathSegmentIndexMap.TryGetValue(actionParameter.ApiParameterName, out int currentPathSegmentIndex))
                    return;

                bool insertBefore = _previousPathSegmentIndex > currentPathSegmentIndex;
                _previousPathSegmentIndex = currentPathSegmentIndex;
                if (insertBefore)
                {
                    index = _previousPathParameterIndex;
                    _previousPathParameterIndex = index;
                }
                _previousPathParameterIndex++;

            }
            else
                index = _actionTargetDefinition.Parameters.Count;

            _actionTargetDefinition.Parameters.Insert(index, actionParameter);
        }
    }
}