using System.Collections.Generic;
using System.Data;

namespace Dibix
{
    internal static class SingleExtensions
    {
        internal static T Single<T>(this IEnumerable<T> result, string commandText, CommandType commandType, ParametersVisitor parameters, bool defaultIfEmpty, bool collectTSqlDebugStatement)
        {
            using IEnumerator<T> enumerator = result.GetEnumerator();

            T current;

            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                if (enumerator.MoveNext())
                    throw DatabaseAccessException.Create(DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement, commandText, commandType, parameters, collectTSqlDebugStatement);
            }
            else if (defaultIfEmpty)
            {
                return default;
            }
            else
            {
                throw DatabaseAccessException.Create(DatabaseAccessErrorCode.SequenceContainsNoElements, commandText, commandType, parameters, collectTSqlDebugStatement);
            }

            return current;
        }
    }
}