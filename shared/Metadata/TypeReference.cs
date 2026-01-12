using System;

namespace Dibix
{
    public abstract class TypeReference : IEquatable<TypeReference>
    {
        public bool IsNullable { get; set; }
        public bool IsEnumerable { get; }
        public abstract string DisplayName { get; }
        public SourceLocation Location { get; }

        protected TypeReference(bool isNullable, bool isEnumerable, SourceLocation location)
        {
            IsNullable = isNullable;
            IsEnumerable = isEnumerable;
            Location = location;
        }

        public bool Equals(TypeReference other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            bool equals = DisplayName == other.DisplayName;
            return equals;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            bool equals = Equals((TypeReference)obj);
            return equals;
        }

        public override int GetHashCode() => DisplayName.GetHashCode();

        public static bool operator ==(TypeReference left, TypeReference right) => Equals(left, right);

        public static bool operator !=(TypeReference left, TypeReference right) => !Equals(left, right);
    }
}