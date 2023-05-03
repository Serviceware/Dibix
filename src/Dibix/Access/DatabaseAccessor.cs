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

        T IDatabaseAccessor.QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QuerySingle<T>(commandText, commandType, parameters));

        Task<T> IDatabaseAccessor.QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QuerySingleAsync<T>(commandText, commandType, parameters, cancellationToken).PostProcess());

        public TReturn QuerySingle<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () => QueryManyCore<TReturn>(commandText, commandType, parameters, types, splitOn).Single());

        T IDatabaseAccessor.QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QuerySingleOrDefault<T>(commandText, commandType, parameters).PostProcess());

        Task<T> IDatabaseAccessor.QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QuerySingleOrDefaultAsync<T>(commandText, commandType, parameters, cancellationToken).PostProcess());

        public TReturn QuerySingleOrDefault<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new() => Execute(commandText, commandType, parameters, () => QueryManyCore<TReturn>(commandText, commandType, parameters, types, splitOn).SingleOrDefault());

        IMultipleResultReader IDatabaseAccessor.QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters) => Execute(commandText, commandType, parameters, () => QueryMultiple(commandText, commandType, parameters));
        
        Task<IMultipleResultReader> IDatabaseAccessor.QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken) => Execute(commandText, commandType, parameters, () => QueryMultipleAsync(commandText, commandType, parameters, cancellationToken));
        #endregion

        #region Abstract Methods
        protected abstract int Execute(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout);

        protected abstract Task<int> ExecuteAsync(string commandText, CommandType commandType, ParametersVisitor parameters, int? commandTimeout, CancellationToken cancellationToken);

        protected abstract IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters);
        
        protected abstract IEnumerable<T> QueryMany<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered);
        
        protected abstract Task<IEnumerable<T>> QueryManyAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, bool buffered, CancellationToken cancellationToken);

        protected abstract IEnumerable<TReturn> QueryMany<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, Func<object[], TReturn> map, string splitOn);

      //protected abstract T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters);

      //protected abstract Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);

        protected abstract T QuerySingleOrDefault<T>(string commandText, CommandType commandType, ParametersVisitor parameters);
        
        protected abstract Task<T> QuerySingleOrDefaultAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);

        protected abstract IMultipleResultReader QueryMultiple(string commandText, CommandType commandType, ParametersVisitor parameters);
        
        protected abstract Task<IMultipleResultReader> QueryMultipleAsync(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken);
        #endregion

        #region Protected Methods
        protected virtual void DisposeConnection() => Connection?.Dispose();

        protected virtual void OnInfoMessage(string message) { }
        #endregion

        #region Private Methods
        private IEnumerable<TReturn> QueryManyCore<TReturn>(string commandText, CommandType commandType, ParametersVisitor parameters, Type[] types, string splitOn) where TReturn : new()
        {
            ValidateParameters(types, splitOn);
            MultiMapper multiMapper = new MultiMapper();
            bool useProjection = types[0] != typeof(TReturn);
            return QueryMany(commandText, commandType, parameters, types, x => multiMapper.MapRow<TReturn>(useProjection, x), splitOn).PostProcess(multiMapper);
        }

        private T QuerySingle<T>(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            IEnumerable<T> result = QueryMany<T>(commandText, commandType, parameters, buffered: false);
            return QuerySingle(result, commandText, commandType, parameters);
        }
        private async Task<T> QuerySingleAsync<T>(string commandText, CommandType commandType, ParametersVisitor parameters, CancellationToken cancellationToken)
        {
            IEnumerable<T> result = await QueryManyAsync<T>(commandText, commandType, parameters, buffered: false, cancellationToken).ConfigureAwait(false);
            return QuerySingle(result, commandText, commandType, parameters);
        }
        private T QuerySingle<T>(IEnumerable<T> result, string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            using IEnumerator<T> enumerator = result.GetEnumerator();

            T current;

            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                if (enumerator.MoveNext())
                    throw CreateException(DatabaseAccessErrorCode.SequenceContainsMoreThanOneElement, commandText, commandType, parameters);
            }
            else
            {
                throw CreateException(DatabaseAccessErrorCode.SequenceContainsNoElements, commandText, commandType, parameters);
            }

            return current;
        }

        private T Execute<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<T> action)
        {
            try { return action(); }
            catch (DatabaseAccessException exception) when (exception.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw; }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, _isSqlClient); }
        }
        private async Task<T> Execute<T>(string commandText, CommandType commandType, ParametersVisitor parameters, Func<Task<T>> action)
        {
            try { return await action().ConfigureAwait(false); }
            catch (DatabaseAccessException exception) when (exception.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw; }
            catch (AggregateException exception) when (exception.InnerException is DatabaseAccessException databaseAccessException && databaseAccessException.AdditionalErrorCode != DatabaseAccessErrorCode.None) { throw databaseAccessException; }
            catch (AggregateException exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception.InnerException ?? exception, _isSqlClient); }
            catch (Exception exception) { throw DatabaseAccessException.Create(commandType, commandText, parameters, exception, _isSqlClient); }
        }

        private void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            TraceSource.TraceInformation(e.Message);
            OnInfoMessage(e.Message);
        }

        private Exception CreateException(DatabaseAccessErrorCode errorCode, string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            string message = CollectExceptionMessage(errorCode);
            return DatabaseAccessException.Create(message, commandType, commandText, parameters, errorCode, _isSqlClient);
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