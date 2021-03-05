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
        int Execute(string sql, CommandType commandType, int? commandTimeout, IParametersVisitor parameters);
        Task<int> ExecuteAsync(string sql, CommandType commandType, int? commandTimeout, IParametersVisitor parameters, CancellationToken cancellationToken);
        IEnumerable<T> QueryMany<T>(string sql, CommandType commandType, IParametersVisitor parameters);
        Task<IEnumerable<T>> QueryManyAsync<T>(string sql, CommandType commandType, IParametersVisitor parameters, bool buffered, CancellationToken cancellationToken);
        IEnumerable<TReturn> QueryMany<TReturn, TSecond>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new();
        IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new();
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn);
        T QuerySingle<T>(string sql, CommandType commandType, IParametersVisitor parameters);
        Task<T> QuerySingleAsync<T>(string sql, CommandType commandType, IParametersVisitor parameters, CancellationToken cancellationToken);
        TReturn QuerySingle<TReturn, TSecond>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new();
        TReturn QuerySingle<TReturn, TSecond, TThird>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new();
        TReturn QuerySingle<TReturn, TSecond, TThird, TFourth>(string sql, CommandType commandType, IParametersVisitor parameters, string splitOn) where TReturn : new();
        T QuerySingleOrDefault<T>(string sql, CommandType commandType, IParametersVisitor parameters);
        IMultipleResultReader QueryMultiple(string sql, CommandType commandType, IParametersVisitor parameters);
        Task<IMultipleResultReader> QueryMultipleAsync(string sql, CommandType commandType, IParametersVisitor parameters, CancellationToken cancellationToken);
    }
}