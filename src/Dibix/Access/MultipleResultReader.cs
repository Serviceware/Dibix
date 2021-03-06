using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Dibix
{
    public abstract class MultipleResultReader : IMultipleResultReader, IDisposable
    {
        #region Fields
        private readonly string _commandText;
        private readonly CommandType _commandType;
        private readonly ParametersVisitor _parameters;
        #endregion

        #region Constructor
        protected MultipleResultReader(string commandText, CommandType commandType, ParametersVisitor parameters)
        {
            this._commandText = commandText;
            this._commandType = commandType;
            this._parameters = parameters;
        }
        #endregion

        #region IMultipleResultReader Members
        public abstract bool IsConsumed { get; }
        IEnumerable<T> IMultipleResultReader.ReadMany<T>() => this.Execute(() => this.ReadMany<T>().PostProcess());

        // TaskReminder
        Task<IEnumerable<T>> IMultipleResultReader.ReadManyAsync<T>() => this.Execute(() => this.ReadManyAsync<T>().PostProcess());

        // ObjectManagement (GetDetailConfigurationExportById, GetDetailConfigurationExportByObjectDef)
        public IEnumerable<TReturn> ReadMany<TReturn, TSecond>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(useProjection: false, a, b), splitOn)
                       .PostProcess(multiMapper);
        });

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> map, string splitOn) => this.Execute(() => this.ReadMany(map, splitOn).PostProcess());

        public IEnumerable<TReturn> ReadManyProjection<TFirst, TSecond, TReturn>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TFirst, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(useProjection: true, a, b), splitOn)
                       .PostProcess(multiMapper);
        });

        // Dibix (Inline_GridResult_WithProjection)
        public IEnumerable<TReturn> ReadManyProjection<TFirst, TSecond, TThird, TReturn>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TFirst, TSecond, TThird, TReturn>((a, b, c) => multiMapper.MapRow<TReturn>(useProjection: true, a, b, c), splitOn)
                       .PostProcess(multiMapper);
        });

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> map, string splitOn) => this.Execute(() => this.ReadMany(map, splitOn).PostProcess());

        // OrderManagement (LoadProductRuntime)
        public IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TReturn>((a, b, c) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c), splitOn)
                       .PostProcess(multiMapper);
        });

        // OrderManagement (GetProduct)
        public IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TFourth, TReturn>((a, b, c, d) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d), splitOn)
                       .PostProcess(multiMapper);
        });

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn) => this.Execute(() => this.ReadMany(map, splitOn).PostProcess());

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn) => this.Execute(() => this.ReadMany(map, splitOn).PostProcess());

        // OrderManagement (GetProductDesign)
        public IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>((a, b, c, d, e, f) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d, e, f), splitOn)
                       .PostProcess(multiMapper);
        });

        // UserManagement (GetCaseAccessGraphConfiguration)
        public IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>((a, b, c, d, e, f, g) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d, e, f, g), splitOn)
                       .PostProcess(multiMapper);
        });

        // UserManagement (GetCaseAccessGraphConfiguration)
        public IEnumerable<TReturn> ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>((a, b, c, d, e, f, g, h, i) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d, e, f, g, h, i), splitOn)
                       .PostProcess(multiMapper);
        });

        IEnumerable<TReturn> IMultipleResultReader.ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn) => this.Execute(() => this.ReadMany(map, splitOn).PostProcess());

        T IMultipleResultReader.ReadSingle<T>() => this.Execute(() => this.ReadSingle<T>().PostProcess());

        // ObjectManagement (LoadGroupACLForSystemIdOrObjectId)
        Task<T> IMultipleResultReader.ReadSingleAsync<T>() => this.Execute(() => this.ReadSingleAsync<T>().PostProcess());

        public TReturn ReadSingle<TReturn, TSecond>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(useProjection: false, a, b), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        // Dibix (Inline_GridResult)
        public TReturn ReadSingle<TReturn, TSecond, TThird>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TReturn>((a, b, c) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        // OrderManagement (LoadProductRuntime)
        public TReturn ReadSingle<TReturn, TSecond, TThird, TFourth>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TFourth, TReturn>((a, b, c, d) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        // OrderManagement (GetProduct)
        public TReturn ReadSingle<TReturn, TSecond, TThird, TFourth, TFifth>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TReturn>((a, b, c, d, e) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d, e), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        // OrderManagement (GetProductDesign)
        public TReturn ReadSingle<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>((a, b, c, d, e, f, g, h, i) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c, d, e, f, g, h, i), splitOn)
                       .PostProcess(multiMapper)
                       .Single();
        });

        T IMultipleResultReader.ReadSingleOrDefault<T>() => this.Execute(() => this.ReadSingleOrDefault<T>().PostProcess());

        // AI (GetConfiguration)
        public TReturn ReadSingleOrDefault<TReturn, TSecond>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TReturn>((a, b) => multiMapper.MapRow<TReturn>(useProjection: false, a, b), splitOn)
                       .PostProcess(multiMapper)
                       .SingleOrDefault();
        });

        // Search (GlobalSearchGroups)
        public TReturn ReadSingleOrDefault<TReturn, TSecond, TThird>(string splitOn) where TReturn : new() => this.Execute(() =>
        {
            MultiMapper multiMapper = new MultiMapper();
            return this.ReadMany<TReturn, TSecond, TThird, TReturn>((a, b, c) => multiMapper.MapRow<TReturn>(useProjection: false, a, b, c), splitOn)
                       .PostProcess(multiMapper)
                       .SingleOrDefault();
        });
        #endregion

        #region Abstract Methods
        protected abstract IEnumerable<T> ReadMany<T>();
        
        protected abstract Task<IEnumerable<T>> ReadManyAsync<T>();

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, string splitOn);
        
        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, string splitOn);

        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TReturn> map, string splitOn);
        
        protected abstract IEnumerable<TReturn> ReadMany<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TReturn> map, string splitOn);

        protected abstract T ReadSingle<T>();

        protected abstract Task<T> ReadSingleAsync<T>();

        protected abstract T ReadSingleOrDefault<T>();
        #endregion

        #region Private Methods
        private T Execute<T>(Func<T> action)
        {
            try { return action(); }
            catch (Exception ex) { throw DatabaseAccessException.Create(this._commandType, this._commandText, this._parameters, ex); }
        }

        #endregion

        #region IDisposable Members
        public abstract void Dispose();
        #endregion
    }
}