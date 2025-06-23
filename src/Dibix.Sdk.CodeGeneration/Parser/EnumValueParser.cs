using System.Collections.Generic;
using System.Text;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal static class EnumValueParser
    {
        public static int? TryParseDynamicValue(IDictionary<string, int> actualValues, string stringValue, SourceLocation location, ILogger logger)
        {
            int actualValue = 0;
            char? bitwiseOperator = null;
            StringBuilder memberNameSb = new StringBuilder();
            int memberNameStart = 0;

            for (int i = 0; i < stringValue.Length; i++)
            {
                char currentToken = stringValue[i];
                bool reachedEnd = i + 1 == stringValue.Length;
                bool reachedOperator = currentToken is '|' or '&';
                bool reachedSpace = currentToken == ' ';
                bool collectMember = reachedEnd || reachedOperator;

                if (reachedSpace)
                {
                    continue;
                }

                if (!reachedOperator)
                {
                    if (memberNameSb.Length == 0)
                        memberNameStart = i;

                    memberNameSb.Append(currentToken);
                }

                if (collectMember)
                {
                    string memberNameReference = memberNameSb.ToString();
                    if (!actualValues.TryGetValue(memberNameReference, out int currentMemberValue))
                    {
                        logger.LogError($"Unexpected enum member name value reference: {memberNameReference}", location.Source, location.Line, location.Column + memberNameStart);
                        return null;
                    }

                    if (bitwiseOperator.HasValue)
                    {
                        switch (bitwiseOperator.Value)
                        {
                            case '|':
                                actualValue |= currentMemberValue;
                                break;

                            case '&':
                                actualValue &= currentMemberValue;
                                break;
                        }

                        bitwiseOperator = null;
                    }
                    else
                        actualValue = currentMemberValue;

                    memberNameSb = new StringBuilder();
                }

                if (reachedOperator)
                {
                    bitwiseOperator = currentToken;
                }
            }

            return actualValue;
        }
    }
}