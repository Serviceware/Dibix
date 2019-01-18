using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    public class TypeInfo
    {
        public TypeName Name { get; }
        public bool IsPrimitiveType { get; }
        public ICollection<string> Properties { get; }

        public TypeInfo(TypeName name, bool isPrimitiveType)
        {
            this.Name = name;
            this.IsPrimitiveType = isPrimitiveType;
            this.Properties = new Collection<string>();
        }

        public static TypeInfo FromClrType(Type type, TypeName typeName)
        {
            TypeInfo info = new TypeInfo(typeName, type.IsPrimitive());
            typeName.CSharpTypeName = type.ToCSharpTypeName();
            foreach (PropertyInfo property in type.GetProperties())
                info.Properties.Add(property.Name);

            return info;
        }
    }
}