using System;
using System.Reflection;
using Dapper;

namespace Dibix.Dapper
{
    internal sealed class DecoratedTypeMap : SqlMapper.ITypeMap
    {
        #region Fields
        private readonly SqlMapper.ITypeMap _inner;
        private readonly Type _type;
        #endregion

        #region Constructor
        private DecoratedTypeMap(SqlMapper.ITypeMap inner, Type type)
        {
            this._inner = inner;
            this._type = type;
        }
        #endregion

        #region Registration
        public static void Adapt<T>() => AdaptCore(typeof(T));
        public static void Adapt<TFirst, TSecond>() => AdaptCore(typeof(TFirst), typeof(TSecond));
        public static void Adapt<TFirst, TSecond, TThird>() => AdaptCore(typeof(TFirst), typeof(TSecond), typeof(TThird));
        public static void Adapt<TFirst, TSecond, TThird, TFourth>() => AdaptCore(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth>() => AdaptCore(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>() => AdaptCore(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>() => AdaptCore(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth>() => AdaptCore(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth));
        public static void Adapt(Type[] types) => AdaptCore(types);
        private static void AdaptCore(params Type[] types)
        {
            foreach (Type type in types)
                Register(type);
        }

        private static void Register(Type type)
        {
            SqlMapper.ITypeMap typeMap = SqlMapper.GetTypeMap(type);
            if (typeMap is DecoratedTypeMap)
                return;

            SqlMapper.SetTypeMap(type, new DecoratedTypeMap(typeMap, type));
        }
        #endregion

        #region SqlMapper.ITypeMap Members
        ConstructorInfo SqlMapper.ITypeMap.FindConstructor(string[] names, Type[] types) => this._inner.FindConstructor(names, types);

        ConstructorInfo SqlMapper.ITypeMap.FindExplicitConstructor() => this._inner.FindExplicitConstructor();

        SqlMapper.IMemberMap SqlMapper.ITypeMap.GetConstructorParameter(ConstructorInfo constructor, string columnName) => this._inner.GetConstructorParameter(constructor, columnName);

        SqlMapper.IMemberMap SqlMapper.ITypeMap.GetMember(string columnName)
        {
            if (String.IsNullOrEmpty(columnName))
                throw new InvalidOperationException($"Column name was not specified, therefore it cannot be mapped to type '{this._type}'");

            SqlMapper.IMemberMap member = this._inner.GetMember(columnName);
            if (member == null)
                throw new InvalidOperationException($"Column '{columnName}' does not match a property on type '{this._type}'");

            return member;
        }
        #endregion
    }
}