using System;
using System.Reflection;
using Dapper;

namespace Dibix.Dapper
{
    internal class DecoratedTypeMap : SqlMapper.ITypeMap
    {
        #region Fields
        private readonly SqlMapper.ITypeMap _inner;
        private readonly Type _type;
        #endregion

        #region Constructor
        private protected DecoratedTypeMap(SqlMapper.ITypeMap inner, Type type)
        {
            _inner = inner;
            _type = type;
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
        ConstructorInfo SqlMapper.ITypeMap.FindConstructor(string[] names, Type[] types) => _inner.FindConstructor(names, types);

        ConstructorInfo SqlMapper.ITypeMap.FindExplicitConstructor() => _inner.FindExplicitConstructor();

        SqlMapper.IMemberMap SqlMapper.ITypeMap.GetConstructorParameter(ConstructorInfo constructor, string columnName) => _inner.GetConstructorParameter(constructor, columnName);

        public virtual SqlMapper.IMemberMap GetMember(string columnName)
        {
            if (String.IsNullOrEmpty(columnName))
                throw new InvalidOperationException($"Column name was not specified, therefore it cannot be mapped to type '{_type}'");

            SqlMapper.IMemberMap member = _inner.GetMember(columnName);
            if (member == null)
                throw new InvalidOperationException($"Column '{columnName}' does not match a property on type '{_type}'");

            return member;
        }
        #endregion
    }
}