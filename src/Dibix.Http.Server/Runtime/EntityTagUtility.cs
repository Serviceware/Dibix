using System;
using System.Collections.Generic;

namespace Dibix.Http.Server
{
    // Parses the entity-tag wire format (RFC 9110 section 8.8.3), so procedures only ever see the raw token
    internal static class EntityTagUtility
    {
        private const string WeakPrefix = "W/";

        // Returns the raw token of the first entity tag in an If-None-Match header, or null if there is none.
        // The weak prefix is ignored, because If-None-Match uses the weak comparison function (RFC 9110 section 13.1.2).
        // '*' yields null, so the procedure falls through to a regular 200 response, which is a valid answer for it.
        public static string ParseFirst(IEnumerable<string> headerValues)
        {
            foreach (string headerValue in headerValues)
            {
                if (headerValue == null)
                    continue;

                int position = 0;
                while (position < headerValue.Length)
                {
                    char current = headerValue[position];
                    if (current == ',' || Char.IsWhiteSpace(current))
                    {
                        position++;
                        continue;
                    }

                    if (current == '*')
                        return null;

                    if (String.CompareOrdinal(headerValue, position, WeakPrefix, 0, WeakPrefix.Length) == 0)
                        position += WeakPrefix.Length;

                    if (position < headerValue.Length && headerValue[position] == '"')
                    {
                        int end = headerValue.IndexOf('"', position + 1);
                        if (end < 0)
                            return null;

                        return headerValue.Substring(position + 1, end - position - 1);
                    }

                    // Lenient: Accept an unquoted token, i.e. sent by a client that does not quote the value
                    int separator = headerValue.IndexOf(',', position);
                    string token = (separator < 0 ? headerValue.Substring(position) : headerValue.Substring(position, separator - position)).Trim();
                    return token.Length > 0 ? token : null;
                }
            }

            return null;
        }
    }
}