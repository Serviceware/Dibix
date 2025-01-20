using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Dibix
{
    public static class SqlDiagnosticsUtility
    {
        internal const string TrimSuffix = "<TRUNCATED>";
        public static int MaxParameterValueLength = 1000;

        internal static string CollectParameterDump(ParametersVisitor parameters, bool truncate)
        {
            string FormatParameter(ParameterDescriptor parameter)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Parameter ").Append(parameter.Name);

                string parameterType;
                string parameterDescription = null;

                if (parameter.Value is StructuredType structuredType)
                {
                    parameterType = structuredType.TypeName;
                    parameterDescription = structuredType.Dump(truncate);
                }
                else
                    parameterType = parameter.Type.ToString();

                if (parameterType != null)
                {
                    sb.Append(' ')
                      .Append(parameterType);

                    if (parameter.Size != null)
                        sb.AppendFormat($"({parameter.Size})");
                }

                sb.Append(":");

                if (parameter.Value is not StructuredType)
                {
                    sb.Append(' ')
                      .Append(TrimParameterValueIfNecessary(parameter.Value, truncate) ?? "NULL");
                }

                if (parameterDescription != null)
                {
                    sb.AppendLine()
                      .Append(parameterDescription);
                }

                string parameterInfo = sb.ToString();
                return parameterInfo;
            }

            ICollection<ParameterDescriptor> parameterDescriptors = CollectParameters(parameters);
            string parameterInfo = String.Join(Environment.NewLine, parameterDescriptors.Select(FormatParameter));
            return parameterInfo;
        }

        internal static string TrimParameterValueIfNecessary(object value, bool trim)
        {
            if (value == null)
                return null;

            string stringValue = value.ToString();
            if (trim && stringValue.Length > MaxParameterValueLength)
                stringValue = $"{stringValue.Substring(0, MaxParameterValueLength)}{TrimSuffix}";

            return stringValue;
        }

        internal static ICollection<ParameterDescriptor> CollectParameters(ParametersVisitor parameters)
        {
            ICollection<ParameterDescriptor> statements = new Collection<ParameterDescriptor>();
            parameters.VisitInputParameters((name, type, value, size, _, _) => statements.Add(new ParameterDescriptor(name, type, value, size)));
            return statements;
        }
    }
}