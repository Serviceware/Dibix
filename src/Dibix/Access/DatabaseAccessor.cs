using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public abstract class DatabaseAccessor : IDatabaseAccessor, IDisposable
    {
        #region Fields
        private static readonly TraceSource TraceSource = new TraceSource("Dibix.Sql");
        private readonly Action _onDispose;
        #endregion

        #region Properties
        protected DbConnection Connection { get; }
        #endregion

        #region Constructor
        protected DatabaseAccessor(DbConnection connection) : this(connection, null) { }
        protected DatabaseAccessor(DbConnection connection, Action onDispose)
        {
            this.Connection = connection;
            if (this.Connection is SqlConnection sqlConnection)
                sqlConnection.InfoMessage += OnInfoMessage;
            
            this._onDispose = onDispose ?? this.DisposeConnection;
        }
        #endregion

        #region IDatabaseAccessor Members
        public IParameterBuilder Parameters() => new ParameterBuilder();

        int IDatabaseAccessor.Execute(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => this.Execute(commandText, commandType, commandTimeout, parameters));

        Task<int> IDatabaseAccessor.ExecuteAsync(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => this.ExecuteAsync(commandText, commandType, commandTimeout, parameters, cancellationToken));

        IEnumerable<T> IDatabaseAccessor.QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => this.QueryMany<T>(commandText, commandType, parameters).PostProcess());
        
        Task<IEnumerable<T>> IDatabaseAccessor.QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => this.QueryManyAsync<T>(commandText, commandType, parameters, buffered, cancellationToken).PostProcess());

        public IEnumerable<TReturn> QueryMany<TReturn, TSecond>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TReturn>(commandText, commandType, parameters, (a, b) => multiMapper.MapRow<TReturn>(useProjection: false, a, b), splitOn)
                       .PostProcess(multiMapper);
        });

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn) => Execute(commandText, commandType, parameters, () => this.QueryMany(commandText, commandType, parameters, map, splitOn).PostProcess());

        public IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TReturn>(commandText, commandType, parameters, (a, b, c) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c), splitOn)
                       .PostProcess(multiMapper);
        });

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn) => Execute(commandText, commandType, parameters, () => this.QueryMany(commandText, commandType, parameters, map, splitOn).PostProcess());

        public IEnumerable<TReturn> QueryMany<TReturn, TSecond, TThird, TFourth>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TFourth, TReturn>(commandText, commandType, parameters, (a, b, c,d ) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d), splitOn)
                       .PostProcess(multiMapper);
        });

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn) => Execute(commandText, commandType, parameters, () => this.QueryMany(commandText, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn) => Execute(commandText, commandType, parameters, () => this.QueryMany(commandText, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn) => Execute(commandText, commandType, parameters, () => this.QueryMany(commandText, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn) => Execute(commandText, commandType, parameters, () => this.QueryMany(commandText, commandType, parameters, map, splitOn).PostProcess());

        IEnumerable<TReturn> IDatabaseAccessor.QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn) => Execute(commandText, commandType, parameters, () => this.QueryMany(commandText, commandType, parameters, map, splitOn).PostProcess());

        T IDatabaseAccessor.QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => this.QuerySingle<T>(commandText, commandType, parameters).PostProcess());

        Task<T> IDatabaseAccessor.QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => this.QuerySingleAsync<T>(commandText, commandType, parameters, cancellationToken).PostProcess());

        public TReturn QuerySingle<TReturn, TSecond>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TReturn>(commandText, commandType, parameters, (a, b) => multiMapper.MapRow<TReturn>(useProjection: false, a, b), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        public TReturn QuerySingle<TReturn, TSecond, TThird>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TReturn>(commandText, commandType, parameters, (a, b, c) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        public TReturn QuerySingle<TReturn, TSecond, TThird, TFourth>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TFourth, TReturn>(commandText, commandType, parameters, (a, b, c, d) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        public TReturn QuerySingle<TReturn, TSecond, TThird, TFourth, TFifth>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TReturn>(commandText, commandType, parameters, (a, b, c, d, e) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d, e), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        public TReturn QuerySingle<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(string commandText, CommandType commandType, ParametersVisitor parameters, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.QueryMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(commandText, commandType, parameters, (a, b, c, d, e, f, g) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d, e, f, g), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        T IDatabaseAccessor.QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => this.QuerySingleOrDefault<T>(commandText, commandType, parameters).PostProcess());

        IMultipleResultReader IDatabaseAccessor.QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => this.QueryMultiple(commandText, commandType, parameters));
        
        Task<IMultipleResultReader> IDatabaseAccessor.QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => this.QueryMultipleAsync(commandText, commandType, parameters, cancellationToken));
        #endregion

        #region Abstract Methods
        protected abstract int Execute(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters);

        protected abstract Task<int> ExecuteAsync(string commandText, CommandType commandType, int? commandTimeout, ParametersVisitor parameters, CancellationToken cancellationToken);

        protected abstract IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters);
        
        protected abstract Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TReturn> map, string splitOn);
        
        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TReturn> map, string splitOn);
        
        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn);
        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> QueryMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh, TReturn> map, string splitOn);

        protected abstract T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters);

        protected abstract Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);

        protected abstract T QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters);

        protected abstract IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters);
        
        protected abstract Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);
        #endregion

        #region Private Methods
        private static T Execute<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<T> action)
        {
            try { return action(); }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception); }
        }
        private static async Task<T> Execute<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<Task<T>> action)
        {
            try { return await action(); }
            catch (AggregateException exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception.InnerException ?? exception); }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception); }
        }

        private static void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            TraceSource.TraceInformation(e.Message);
        }

        private void DisposeConnection() => this.Connection?.Dispose();
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Connection is SqlConnection sqlConnection) 
                    sqlConnection.InfoMessage -= OnInfoMessage;

                this._onDispose?.Invoke();
            }
        }
        #endregion
    }
}