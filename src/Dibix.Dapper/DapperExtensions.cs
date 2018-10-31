using Dapper;

namespace Dibix.Dapper
{
    public static class DapperExtensions
    {
           internal static object AsDapperParams(this IParametersVisitor parametersVisitor)
        {
            Guard.IsNotNull(parametersVisitor, nameof(parametersVisitor));
            DynamicParameters @params = new DynamicParameters();
            parametersVisitor.VisitParameters((name, value, type, suggestedDataType) => @params.Add(name, ProcessParameterValue(value), suggestedDataType));
            return @params;
        }
        private static object ProcessParameterValue(object value)
        {
            const string schemaName = "dbo";
            StructuredType tvp = value as StructuredType;
            if (tvp != null)
                return new DapperStructuredTypeParameter(schemaName, tvp.TypeName, tvp.GetRecords);

            return value;
        }
    }
}