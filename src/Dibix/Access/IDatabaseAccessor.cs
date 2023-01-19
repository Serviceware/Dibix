using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public interface IDatabaseAccessor : IDisposable
    {
        IParameterBuilder Parameters();
        int Execute(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters);
        Task<int> ExecuteAsync(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters, CancellationToken cancellationToken);
        IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters);
        Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken);
        IEnumerable<TReturn> QueryMany<TReturn, TSecond>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new();
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new();
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new();
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn);
        T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters);
        Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);
        TReturn QuerySingle<TReturn, TSecond>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new();
        TReturn QuerySingle<TReturn, TSecond, TThird>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new();
        TReturn QuerySingle<TReturn, TSecond, TThird, TFourth>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new();
        TReturn QuerySingle<TReturn, TSecond, TThird, TFourth, TFifth>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new();
        TReturn QuerySingle<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new();
        TReturn QuerySingle<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new();
        T QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters);
        Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);
        IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters);
        Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);
    }
}