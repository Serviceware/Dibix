using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class EnumValueParser
    {
        public static int ParseDynamicValue(IDictionary<string, int> actualValues, string stringValue)
        {
            int actualValue = 0;

            Queue<char> tokens = new Queue<char>(stringValue);
            char currentToken = default;
            char? bitwiseOperator = null;
            StringBuilder memberNameSb = new StringBuilder();
            while (tokens.Any())
            {
                while (tokens.Any() && (currentToken = tokens.Dequeue()) != default && currentToken != '|' && currentToken != '&')
                {
                    if (currentToken == ' ')
                        continue;

                    memberNameSb.Append(currentToken);
                }

                string memberNameReference = memberNameSb.ToString();
                if (!actualValues.TryGetValue(memberNameReference, out int currentMemberValue))
                    throw new InvalidOperationException($"Unexpected enum member name value reference: {memberNameReference}");

                memberNameSb = new StringBuilder();

                if (bitwiseOperator.HasValue)
                {
                    if (bitwiseOperator.Value == '|')
                        actualValue |= currentMemberValue;
                    else if (bitwiseOperator.Value == '&')
                        actualValue &= currentMemberValue;

                    bitwiseOperator = null;
                }
                else
                    actualValue = currentMemberValue;

                if (currentToken == '|' || currentToken == '&')
                {
                    bitwiseOperator = currentToken;
                }
            }

            return actualValue;
        }
    }
}