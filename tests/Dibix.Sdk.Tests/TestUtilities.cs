using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Dibix.Sdk.CodeAnalysis.Rules;
using Xunit;

namespace Dibix.Sdk.Tests.Utilities
{
    public static class TestUtilities
    {
        public static void AssertEqualWithDiffTool(string expected, string actual)
        {
            if (expected != actual)
                RunWinMerge(expected, actual);

            Assert.Equal(expected, actual);
        }

        public static void DefineNamingConventions()
        {
            string assemblyName = typeof(NamingConventionSqlCodeAnalysisRule).Assembly.GetName().Name;
            Type namingConventionType = Type.GetType($"Dibix.Sdk.CodeAnalysis.Rules.NamingConvention,{assemblyName}", true);
            IDictionary<string, string> overrides = new Dictionary<string, string>
            {
                { "Table",     "dbx*" }
              , { "Procedure", "dbx*" }
            };
            overrides.Each(x => namingConventionType.GetField(x.Key, BindingFlags.Public | BindingFlags.Static).SetValue(null, x.Value));
        }

        private static void RunWinMerge(string expectedText, string actualText)
        {
            string expectedFileName = Path.GetTempFileName();
            string actualFileName = Path.GetTempFileName();
            File.WriteAllText(expectedFileName, expectedText);
            File.WriteAllText(actualFileName, actualText);
            Process.Start("winmerge", $"\"{expectedFileName}\" \"{actualFileName}\"");
        }
    }
}
