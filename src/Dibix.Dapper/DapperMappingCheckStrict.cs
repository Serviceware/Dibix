using System;
using System.Reflection;
using Dapper;

namespace Dibix.Dapper
{
    internal sealed class DapperMappingCheckStrict : DapperMappingCheck
    {
        internal override void Check(params Type[] types)
        {
            foreach (Type type in types)
                Register(type);
        }

        private static void Register(Type type)
        {
            SqlMapper.ITypeMap typeMape = SqlMapper.GetTypeMap(type);
            SafeTypeMap safeTypeMap = typeMape as SafeTypeMap;

            // The safe map is already registered
            if (safeTypeMap != null)
                return;

            // Wrap the current map with the a safe map
            safeTypeMap = new SafeTypeMap(type, typeMape);
            SqlMapper.SetTypeMap(type, safeTypeMap);
        }

        private sealed class SafeTypeMap : SqlMapper.ITypeMap
        {
            private readonly Type _type;
            private readonly SqlMapper.ITypeMap _originalTypeMap;

            public SafeTypeMap(Type theType, SqlMapper.ITypeMap originalTypeMap)
            {
                this._type = theType;
                this._originalTypeMap = originalTypeMap;
            }

            public ConstructorInfo FindConstructor(string[] names, Type[] types)
            {
                return this._originalTypeMap.FindConstructor(names, types);
            }

            public ConstructorInfo FindExplicitConstructor()
            {
                return this._originalTypeMap.FindExplicitConstructor();
            }

            public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName)
            {
                return this._originalTypeMap.GetConstructorParameter(constructor, columnName);
            }

            public SqlMapper.IMemberMap GetMember(string columnName)
            {
                if (String.IsNullOrEmpty(columnName))
                    throw new InvalidOperationException($"Column name was not specified, therefore it cannot be mapped to type '{this._type}'");

                SqlMapper.IMemberMap member = this._originalTypeMap.GetMember(columnName);
                if (member == null)
                    throw new InvalidOperationException($"Column '{columnName}' does not match a property on type '{this._type}'");
                
                return member;
            }
        }
    }
}