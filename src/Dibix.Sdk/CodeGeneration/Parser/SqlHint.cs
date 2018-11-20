﻿using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    internal class SqlHint
    {
        public const string Default = "";
        public const string Name = "Name";
        public const string Return = "Return";
        public const string ResultTypeName = "ResultTypeName";
        public const string ClrType = "ClrType";
        public const string Nullable = "Nullable";

        public string Kind { get; set; }
        public string Value
        {
            get
            {
                if (this.Properties.TryGetValue(Default, out var value))
                    return value;

                return null;
            }
        }
        public IDictionary<string, string> Properties { get; }
        public int Line { get; set; }
        public int Column { get; set; }

        public SqlHint(string kind, int line, int column)
        {
            this.Kind = kind;
            this.Line = line;
            this.Column = column;
            this.Properties = new Dictionary<string, string>();
        }
    }
}