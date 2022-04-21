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
        public static void Adapt<T>() => Adapt(typeof(T));
        public static void Adapt<TFirst, TSecond>() => Adapt(typeof(TFirst), typeof(TSecond));
        public static void Adapt<TFirst, TSecond, TThird>() => Adapt(typeof(TFirst), typeof(TSecond), typeof(TThird));
        public static void Adapt<TFirst, TSecond, TThird, TFourth>() => Adapt(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth>() => Adapt(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth>() => Adapt(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh>() => Adapt(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>() => Adapt(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth>() => Adapt(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth), typeof(TTenth));
        public static void Adapt<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh>() => Adapt(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth), typeof(TTenth), typeof(TEleventh));
        private static void Adapt(params Type[] types)
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