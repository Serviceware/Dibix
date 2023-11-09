using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix
{
    public abstract class DatabaseAccessor : IDatabaseAccessor, IDisposable
    {
        #region Fields
        private static readonly TraceSource TraceSource = new TraceSource("Dibix.Sql");
        private readonly bool _isSqlClient;
        #endregion

        #region Properties
        protected DbConnection Connection { get; }
        #endregion

        #region Constructor
        protected DatabaseAccessor(DbConnection connection)
        {
            _isSqlClient = false;
            Connection = connection;

            if (Connection is SqlConnection sqlConnection)
            {
                sqlConnection.InfoMessage += OnInfoMessage;
                _isSqlClient = true;
            }
        }
        #endregion

        #region IDatabaseAccessor Members
        public IParameterBuilder Parameters() => new ParameterBuilder();

        int IDatabaseAccessor.Execute(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => Execute(commandText, commandType, parameters, commandTimeout: null));
        
        int IDatabaseAccessor.Execute(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout) => Execute(commandText, commandType, parameters, () => Execute(commandText, commandType, parameters, commandTimeout));

        Task<int> IDatabaseAccessor.ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => ExecuteAsync(commandText, commandType, parameters, commandTimeout: null, cancellationToken));
        
        Task<int> IDatabaseAccessor.ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => ExecuteAsync(commandText, commandType, parameters, commandTimeout, cancellationToken));

        IEnumerable<T> IDatabaseAccessor.QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QueryMany<T>(commandText, commandType, parameters).PostProcess());
        
        Task<IEnumerable<T>> IDatabaseAccessor.QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QueryManyAsync<T>(commandText, commandType, parameters, buffered: true, cancellationToken).PostProcess());
        
        Task<IEnumerable<T>> IDatabaseAccessor.QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QueryManyAsync<T>(commandText, commandType, parameters, buffered, cancellationToken).PostProcess());
        
        public IEnumerable<TReturn> QueryMany<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () => QueryManyCore<TReturn>(commandText, commandType, parameters, types, splitOn));

        T IDatabaseAccessor.QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QuerySingle<T>(commandText, commandType, parameters, defaultIfEmpty: false).PostProcess());

        Task<T> IDatabaseAccessor.QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QuerySingleAsync<T>(commandText, commandType, parameters, defaultIfEmpty: false, cancellationToken).PostProcess());

        public TReturn QuerySingle<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () => QuerySingle<TReturn>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: false));

        T IDatabaseAccessor.QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QuerySingle<T>(commandText, commandType, parameters, defaultIfEmpty: true).PostProcess());

        Task<T> IDatabaseAccessor.QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QuerySingleAsync<T>(commandText, commandType, parameters, defaultIfEmpty: true, cancellationToken).PostProcess());

        public TReturn QuerySingleOrDefault<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () => QuerySingle<TReturn>(commandText, commandType, parameters, types, splitOn, defaultIfEmpty: true));

        IMultipleResultReader IDatabaseAccessor.QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QueryMultiple(commandText, commandType, parameters));
        
        Task<IMultipleResultReader> IDatabaseAccessor.QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QueryMultipleAsync(commandText, commandType, parameters, cancellationToken));
        #endregion

        #region Abstract Methods
        protected abstract int Execute(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout);

        protected abstract Task<int> ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, CancellationToken cancellationToken);

        protected abstract IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters);
        
        protected abstract IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered);
        
        protected abstract Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken);

        protected abstract IEnumerable<TReturn> QueryMany<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, Func<object[], TReturn> map, string splitOn, bool buffered);

      //protected abstract T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters);

      //protected abstract Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);

      //protected abstract T QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters);
        
      //protected abstract Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);

        protected abstract IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters);
        
        protected abstract Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);
        #endregion

        #region Protected Methods
        protected virtual void DisposeConnection() => Connection?.Dispose();

        protected virtual void OnInfoMessage(string message) { }
        #endregion

        #region Private Methods
        private IEnumerable<TReturn> QueryManyCore<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, bool buffered = true) where TReturn : new()
        {
            ValidateParameters(types, splitOn);
            MultiMapper multiMapper = new MultiMapper();
            bool useProjection = types[0] != typeof(TReturn);
            return QueryMany(commandText, commandType, parameters, types, x => multiMapper.MapRow<TReturn>(useProjection, x), splitOn, buffered).PostProcess(multiMapper);
        }

        private T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool defaultIfEmpty)
        {
            IEnumerable<T> result = QueryMany<T>(commandText, commandType, parameters, buffered: false);
            return QuerySingle(result, commandText, commandType, parameters, defaultIfEmpty);
        }
        private T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn, bool defaultIfEmpty) where T : new()
        {
            IEnumerable<T> result = QueryManyCore<T>(commandText, commandType, parameters, types, splitOn, buffered: false);
            return QuerySingle(result, commandText, commandType, parameters, defaultIfEmpty);
        }
        private async Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool defaultIfEmpty, CancellationToken cancellationToken)
        {
            IEnumerable<T> result = await QueryManyAsync<T>(commandText, commandType, parameters, buffered: false, cancellationToken).ConfigureAwait(false);
            return QuerySingle(result, commandText, commandType, parameters, defaultIfEmpty);
        }
        private T QuerySingle<T>(IEnumerable<T> result, string commandText, CommandType commandType, ParametersVisitor parameters, bool defaultIfEmpty)
        {
            using IEnumerator<T> enumerator = result.GetEnumerator();

            T current;

            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                if (enumerator.MoveNext())
                    throw CreateException(DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement, commandText, commandType, parameters);
            }
            else if (defaultIfEmpty)
            {
                return default;
            }
            else
            {
                throw CreateException(DatabaseAccessErrorCode.SequenceContainsNoElements, commandText, commandType, parameters);
            }

            return current;
        }

        private T Execute<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<T> action)
        {
            try 
            {
                ValidateParameters(commandText, commandType, parameters);
                return action();
            }
            catch (DatabaseAccessException exception) when (exception.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw; }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, _isSqlClient); }
        }
        private async Task<T> Execute<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<Task<T>> action)
        {
            try
            {
                ValidateParameters(commandText, commandType, parameters);
                return await action().ConfigureAwait(false);
            }
            catch (DatabaseAccessException exception) when (exception.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw; }
            catch (AggregateException exception) when (exception.InnerException is DatabaseAccessException databaseAccessException && databaseAccessException.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw databaseAccessException; }
            catch (AggregateException exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception.InnerException ?? exception, _isSqlClient); }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, _isSqlClient); }
        }

        private void ValidateParameters(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            parameters.VisitInputParameters((name, type, value, size, _, _) =>
            {
                ValidateSize(name, type, value, size, commandText, commandType, parameters);
            });
        }

        private void ValidateSize(string name, DbType type, object value, int? size, string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            if (size == null)
                return;

            switch (type)
            {
                case DbType.String:
                case DbType.AnsiString:
                case DbType.StringFixedLength:
                case DbType.AnsiStringFixedLength:
                    if (value is string str && str.Length > size)
                        throw CreateException(DatabaseAccessErrorCode.ParameterSizeExceeded, $"Length of parameter '{name}' is '{str.Length}', which exceeds the supported size '{size}'", commandText, commandType, parameters);

                    return;

                default:
                    return;
            }
        }

        private Exception CreateException(DatabaseAccessErrorCode errorCode, string commandText, CommandType commandType, ParametersVisitor parameters) => CreateException(errorCode, CollectExceptionMessage(errorCode), commandText, commandType, parameters);
        private Exception CreateException(DatabaseAccessErrorCode errorCode, string message, string commandText, CommandType commandType, ParametersVisitor parameters) => DatabaseAccessException.Create(message, commandType, commandText, parameters, errorCode, _isSqlClient);

        private void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            TraceSource.TraceInformation(e.Message);
            OnInfoMessage(e.Message);
        }

        private static void ValidateParameters(IReadOnlyCollection<Type> types, string splitOn)
        {
            MultiMapUtility.ValidateParameters(types, splitOn);
        }

        private static string CollectExceptionMessage(DatabaseAccessErrorCode errorCode) => errorCode switch
        {
            DatabaseAccessErrorCode.SequenceContainsNoElements => "Sequence contains no elements",
            DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement => "Sequence contains more than one element",
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null)
        };
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Connection is SqlConnection sqlConnection) 
                    sqlConnection.InfoMessage -= OnInfoMessage;

                DisposeConnection();
            }
        }
        #endregion
    }
}