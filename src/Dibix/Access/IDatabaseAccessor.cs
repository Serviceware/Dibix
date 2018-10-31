using System;
using System.Collections.Generic;
using System.Data;

namespace Dibix
{
    public interface IDatabaseAccessor : IDisposable
    {
        IParameterBuilder Parameters();
        int Execute(string sql, CommandType commandType, IParametersVisitor parameters);
        T ExecuteScalar<T>(string sql, CommandType commandType, IParametersVisitor parameters);
        IEnumerable<T> QueryMany<T>(string sql, CommandType commandType, IParametersVisitor parameters);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn);
        IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, CommandType commandType, IParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn);
        T QuerySingle<T>(string sql, CommandType commandType, IParametersVisitor parameters);
        T QuerySingleOrDefault<T>(string sql, CommandType commandType, IParametersVisitor parameters);
        IMultipleResultReader QueryMultiple(string sql, CommandType commandType, IParametersVisitor parameters);
    }
}